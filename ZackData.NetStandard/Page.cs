using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class Page<T>
    {
        public long TotalPages { get; set; }
        public long TotalElements { get; set; }

        /// <summary>
        /// zero-based
        /// </summary>
        public long PageNumber { get; set; }
        public int PageSize { get; set; }
        public IQueryable<T> Content { get; set; }
    }
}
