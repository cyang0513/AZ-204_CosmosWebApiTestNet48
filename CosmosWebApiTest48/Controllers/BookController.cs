using CosmosWebApiTest48.Models;
using Microsoft.Azure.Cosmos;
using Swashbuckle.Swagger.Annotations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using TRex.Metadata;

namespace CosmosWebApiTest48.Controllers
{
   public class BookController : ApiController
   {
      static Container m_Container;
      static readonly Dictionary<string, Callback> m_Callbacks = new Dictionary<string, Callback>();

      public BookController()
      {
         Trace.AutoFlush = true;
         if (m_Container == null)
         {
            var client = new CosmosClient(ConfigurationManager.ConnectionStrings["Cosmos"].ConnectionString);
            var db = client.GetDatabase("ChyaTestDB");

            var containerTask = db.CreateContainerIfNotExistsAsync(new ContainerProperties("Book", "/Category"));
            containerTask.Wait();
            m_Container = containerTask.Result.Container;
         }
      }

      [HttpGet, Route("Book/All")]
      [Metadata("Get all books", "Get all the books objects stored in the App")]
      [SwaggerResponse(HttpStatusCode.OK, "An array of books", typeof(Array))]
      public IList<Book> GetAllBook()
      {
         var res = m_Container.GetItemLinqQueryable<Book>(true).Select(x => x).ToList();
         return res;
      }

      [HttpGet, Route("Book/{name}", Name = "GetBook")]
      [Metadata("Get book by name", "Get book objects by name.")]
      [SwaggerResponse(HttpStatusCode.OK, "An object represeting a list of books", typeof(Book))]
      public IList<Book> Details(string name)
      {
         return m_Container.GetItemLinqQueryable<Book>(true).Where(x => x.BookName == name).ToList();
      }

      [HttpPost, Route("Book/Add")]
      [Metadata("Add a new book", "Add a new book.")]
      [SwaggerResponse(HttpStatusCode.Created)]
      public async Task<Book> Create(Book book)
      {
         if (m_Container.GetItemLinqQueryable<Book>(true).Count(x => x.BookName == book.BookName) == 0)
         {
            var bookCreate = await m_Container.CreateItemAsync<Book>(book, new PartitionKey(book.Category));

            foreach (var call in m_Callbacks.Values)
            {
               Trace.Write($"Callback invoked {call.Id} - {call.Uri}");
               call.InvokeAsync<Book>(bookCreate.Resource);
            }

            return bookCreate.Resource;
         }

         return null;
      }

      [HttpDelete, Route("Book/Delete")]
      [Metadata("Delete a book", "Delete a book object by its id and category.")]
      public async Task<string> Delete(string id, string catrgory)
      {
         try
         {
            await m_Container.DeleteItemAsync<Book>(id, new PartitionKey(catrgory));
            return $"Document {id} set to delete";
         }
         catch (Exception ex)
         {
            return ex.Message;
         }

      }

      // Subscribe to newly created books
      [Metadata("New book created", "Fires whenever a new book is added to the list.", VisibilityType.Important)]
      [Trigger(TriggerType.Subscription, typeof(Book), "Book")]
      [SwaggerResponseRemoveDefaults]
      [SwaggerResponse(HttpStatusCode.Created, "Subscription created")]
      [SwaggerResponse(HttpStatusCode.BadRequest, "Call back exists")]
      [HttpPost, Route("Book/Subscribe")]
      public IHttpActionResult Subscribe(Callback callback)
      {
         if (m_Callbacks.ContainsKey(callback.Id))
         {
            return BadRequest();
         }
         if (m_Callbacks.Values.Any(x=>x.Uri.ToString() == callback.Uri.ToString()))
         {
            return BadRequest();
         }
         m_Callbacks.Add(callback.Id, callback);
         Trace.Write($"Callback added {callback.Id} - {callback.Uri}");
         return CreatedAtRoute(nameof(Unsubscribe), new
         {
            subscriptionId = callback.Id
         }, string.Empty);
      }

      [HttpDelete, Route("Book/Subscribe/{callbackId}", Name = nameof(Unsubscribe))]
      [Metadata("Unsubscribe", Visibility = VisibilityType.Internal)]
      [SwaggerResponse(HttpStatusCode.OK)]
      [SwaggerResponse(HttpStatusCode.BadRequest, "Invalid callbackId")]
      public IHttpActionResult Unsubscribe(string callbackId)
      {
         if (m_Callbacks.ContainsKey(callbackId))
         {
            Trace.Write($"Callback removed {callbackId}");
            m_Callbacks.Remove(callbackId);
            return Ok();
         }
         return BadRequest();
      }
   }
}