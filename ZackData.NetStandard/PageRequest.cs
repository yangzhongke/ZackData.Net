using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public class PageRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Offset { get; set; }
        public Sort Sort { get; set; }
    }
}
