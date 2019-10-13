using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities
{
    [ToString]
    public class Book
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        public DateTime PublishDate { get; set; }
        public long AuthorId { get; set; }
        public Author Author { get; set; }    
        public bool IsDomestic { get; set; }
    }
}
