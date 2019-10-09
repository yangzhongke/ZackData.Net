using System;
using System.Collections.Generic;
using System.Text;

namespace YouZack.Entities
{
    [ToString]
    public class Author
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public IList<Book> Books { get; set; } = new List<Book>();
    }
}
