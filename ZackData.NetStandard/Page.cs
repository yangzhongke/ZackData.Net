using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class Page<T>
    {
        public int TotalPages { get; set; }
        public int TotalElements { get; set; }

        public int PageNumberOfCurrent { get; set; }
        public int PageSize { get; set; }
        public int NumberOfCurrentElements { get; set; }
        public IEnumerable<T> Content { get; set; } = new List<T>();
        public Sort Sort { get; set; }

        public bool IsFirst { get; set; }

        public bool IsLast { get; set; }
    }
}
