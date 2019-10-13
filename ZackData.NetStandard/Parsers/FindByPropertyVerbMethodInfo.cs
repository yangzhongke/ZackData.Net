using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    class FindByPropertyVerbMethodInfo : FindMethodBaseInfo
    {
        public string PropertyName { get; set; }
        public PropertyVerb Verb { get; set; }

        public enum PropertyVerb
        {
            Unkown,
            Between,
            LessThan,
            LessThanEqual,
            GreaterThan,
            GreaterThanEqual,
            After,
            Before,
            IsNull,
            IsNotNull,
            Like,
            NotLike,
            StartingWith,
            EndingWith,
            Containing,
            Not,
            In,
            NotIn,
            True,
            False
        }
    }
}
