using System;
using System.Collections.Generic;
using System.Text;

namespace Tests.NetCore
{
    [ToString]
    public class BookAuthorModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string AuthorName { get; set; }
    }
}
