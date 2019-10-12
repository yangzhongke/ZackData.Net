using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =true)]
    public class PredicateAttribute:Attribute
    {
        public string QueryExpression { get; private set; }
        public PredicateAttribute(string queryExpression)
        {
            this.QueryExpression = queryExpression;
        }
    }
}
