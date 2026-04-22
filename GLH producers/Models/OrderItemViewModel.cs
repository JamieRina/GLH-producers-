namespace GLH_producers.Models
{
    public class OrderItemViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }
}
