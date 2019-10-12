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

        public long PageNumberOfCurrent { get; set; }
        public int PageSize { get; set; }
        public int NumberOfCurrentElements { get; set; }
        public IQueryable<T> Content { get; set; }
        public Sort Sort { get; set; }

        public bool IsFirst { get; set; }

        public bool IsLast { get; set; }
    }
}
