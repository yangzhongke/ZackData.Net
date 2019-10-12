using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class NativeSQLAttribute : Attribute
    {
        public string Sql { get; private set; }
        public NativeSQLAttribute(string sql)
        {
            this.Sql = sql;
        }
    }
}
