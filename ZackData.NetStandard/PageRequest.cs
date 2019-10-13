using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class PageRequest
    {
        /// <summary>
        /// zero-based
        /// </summary>
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public Order[] Orders { get; set; }
    }
}
