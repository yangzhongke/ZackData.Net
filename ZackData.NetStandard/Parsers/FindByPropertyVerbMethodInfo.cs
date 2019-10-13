using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    class FindByPropertyVerbMethodInfo : FindMethodBaseInfo
    {
        public string PropertyName { get; set; }
        public PropertyVerb Verb { get; set; }
    }
}
