using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    [AttributeUsage(AttributeTargets.Method,AllowMultiple =false,Inherited =true)]
    public class PredicateAttribute:Attribute
    {
        public string Predicate { get; private set; }
        public PredicateAttribute(string predicate)
        {
            this.Predicate = predicate;
        }
    }
}
