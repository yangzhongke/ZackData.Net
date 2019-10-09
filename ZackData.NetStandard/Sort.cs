using System;
using System.Collections.Generic;
using System.Text;

namespace ZackData.NetStandard
{
    public sealed class Sort
    {
        public Sort()
        {

        }

        public Sort(params Order[] orders)
        {
            foreach(var order in orders)
            {
                Orders.Add(order);
            }
        }
        public IList<Order> Orders { get; set; } = new List<Order>();
    }
}
