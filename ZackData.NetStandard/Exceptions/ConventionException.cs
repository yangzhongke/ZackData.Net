using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard.Exceptions
{
    public class ConventionException:Exception
    {
        public ConventionException(string message) : base(message)
        {
        }
    }
}
