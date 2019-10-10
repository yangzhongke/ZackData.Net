using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Exceptions
{
    public class PropertyNotFoundException:Exception
    {
        public PropertyNotFoundException(string message):base(message)
        {
        }
    }
}
