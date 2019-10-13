using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Parsers
{
    static class ParserHelper
    {
        public static FindByTwoPropertiesMethodInfo.OperatorType ParseOperatorType(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            return (FindByTwoPropertiesMethodInfo.OperatorType)Enum.Parse(typeof(FindByTwoPropertiesMethodInfo.OperatorType), value, true);
        }

        public static FindByPropertyVerbMethodInfo.PropertyVerb ParsePropertyVerb(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            return (FindByPropertyVerbMethodInfo.PropertyVerb)Enum.Parse(typeof(FindByPropertyVerbMethodInfo.PropertyVerb), value, true);
        }
    }
}
