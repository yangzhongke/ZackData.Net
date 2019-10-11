using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NativeQueryAttribute : Attribute
    {
        public string Sql { get; private set; }
        public NativeQueryAttribute(string queryExpression)
        {
            this.Sql = queryExpression;
        }
    }
}
