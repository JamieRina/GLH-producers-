using System.Collections.Generic;
using System.Linq;

namespace GLH_producers.Models
{
    public class BasketViewModel
    {
        public BasketViewModel()
        {
            Items = new List<BasketItem>();
        }

        public List<BasketItem> Items { get; set; }

        public decimal Total
        {
            get { return Items.Sum(item => item.Subtotal); }
        }

        public int ItemCount
        {
            get { return Items.Sum(item => item.Quantity); }
        }
    }
}
