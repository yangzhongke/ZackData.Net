using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class Order
    {
        public bool Ascending { get; set; }
		public String Property { get; set; }

        public static Order Asc(String propertyName)
        {
            return new Order(propertyName, true);
        }

        public static Order Desc(String propertyName)
        {
            return new Order(propertyName, false);
        }

        public Order(String propertyName, bool ascending=false)
        {
            this.Property = propertyName;
            this.Ascending = ascending;
        }
    }
}
