using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =true)]
    public class QueryAttribute:Attribute
    {
        public string QueryExpression { get; private set; }
        public QueryAttribute(string queryExpression)
        {
            this.QueryExpression = queryExpression;
        }
    }
}
