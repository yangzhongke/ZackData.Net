using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    abstract class FindMethodBaseInfo
    {
        public string MethodName { get; set; }

        public Order Order { get; set; }
        public ParameterInfo SortParameter { get; set; }
        public ParameterInfo PageRequestParameter { get; set; }

        private Type returnType;
        public Type ReturnType 
        { 
            get
            {
                return this.returnType;
            }
            set
            {
                if(value==null)
                {
                    throw new ArgumentNullException(nameof(ReturnType));
                }
                this.returnType = value;
                this.ReturnTypeIsEnumerable = false;
                this.ReturnTypeIsIQueryable = false;
                this.ReturnTypeIsPage = false;
                this.ReturnTypeIsSingle = false;
                if (!value.IsGenericType
                   || value.GetGenericTypeDefinition() != typeof(Page<>))
                {
                    this.ReturnTypeIsPage = true;
                }

            }
        }
        public bool ReturnTypeIsPage { get; private set; }
        public bool ReturnTypeIsSingle { get; private set; }
        public bool ReturnTypeIsEnumerable { get; private set; }
        public bool ReturnTypeIsIQueryable { get; private set; }
    }
}
