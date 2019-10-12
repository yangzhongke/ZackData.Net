using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class PageRequest
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public Sort Sort { get; set; }
    }
}
