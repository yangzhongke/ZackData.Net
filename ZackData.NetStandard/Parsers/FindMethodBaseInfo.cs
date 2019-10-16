using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    abstract class FindMethodBaseInfo
    {
        public string MethodName { get; set; }
        
        /// <summary>
        /// FindByNameOrderByAge
        /// </summary>
        public Order OrderInMethodName { get; set; }

        /// <summary>
        /// FindXXX(Order order)
        /// </summary>
        public ParameterInfo OrderParameter { get; set; }

        /// <summary>
        /// FindXXX(Orders orders)
        /// </summary>
        public ParameterInfo OrdersParameter { get; set; }

        //parameters except ones that is of type PageRequest,Order, or Order[]
        public ParameterInfo[] PlainParameters { get; set; }

        public ParameterInfo PageRequestParameter { get; set; }

        public Type ReturnType 
        {
            get;set;
        }
    }
}
