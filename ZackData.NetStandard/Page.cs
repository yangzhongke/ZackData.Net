using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class Page<T>
    {
        public int TotalPages { get; protected set; }
        public int TotalElements { get; protected set; }

        public int PageNumberOfCurrent { get; protected set; }
        public int PageSize { get; protected set; }
        public int NumberOfCurrentElements { get; protected set; }
        public IEnumerable<T> Content { get; set; } = new List<T>();
        public Sort Sort { get; protected set; }

        public bool IsFirst { get; protected set; }

        public bool IsLast { get; protected set; }
    }
}
