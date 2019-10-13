using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    static class CommonHelper
    {
        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string s1, string s2)
        {
            if (s1 == null && s2 == null)
            {
                return true;
            }
            else if (s1 != null)
            {
                return s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return false;
            }
        }

        public static bool ContainsIgnoreCase(this string s1, string s2)
        {
            if (s1 == null && s2 == null)
            {
                return true;
            }
            else if (s1 != null)
            {
                return s1.IndexOf(s2, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            else
            {
                return false;
            }
        }
    }
}
