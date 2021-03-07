using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TRex.Metadata;

namespace CosmosWebApiTest48.Models
{
   public class Book
   {
      public Book()
      {
         this.id = Guid.NewGuid().ToString();
      }
      string bookId;

      [Metadata("Callback ID", Visibility = VisibilityType.Internal)]
      public string id{ get; set; }

      [Metadata("Book name", "The title of the book")]
      public string BookName { set; get; }

      [Metadata("Author name", "The author of the book")]
      public string AuthorName { set; get; }

      [Metadata("Category", "The category of the book")]
      public string Category { get; set; }
   }
}