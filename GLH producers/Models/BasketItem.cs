using System;

namespace GLH_producers.Models
{
    public class BasketItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int Stock { get; set; }

        public decimal Subtotal
        {
            get { return Price * Quantity; }
        }
    }
}
