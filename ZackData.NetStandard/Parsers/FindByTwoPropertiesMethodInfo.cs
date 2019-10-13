using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    class FindByTwoPropertiesMethodInfo : FindMethodBaseInfo
    {
        public string PropertyName1 { get; set; }
        public OperatorType Operator { get; set; }
        public string PropertyName2 { get; set; }

        public enum OperatorType
        {
            Add,Or
        }
    }
}
