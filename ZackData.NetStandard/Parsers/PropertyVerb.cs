using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    enum PropertyVerb
    {
        Between,
        Equals,
        NotEquals,
        LessThan,
        LessThanEqual,
        GreaterThan,
        GreaterThanEqual,
        IsNull,
        IsNotNull,
        StartsWith,
        EndsWith,
        Contains,
        In,
        True,
        False
    }
}
