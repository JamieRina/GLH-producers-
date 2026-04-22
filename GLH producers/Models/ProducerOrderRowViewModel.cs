using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class ProducerOrderRowViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }

        public string CustomerName { get; set; }

        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string Status { get; set; }
        public string OrderType { get; set; }
        public string Address { get; set; }
        public DateTime? RequiredDate { get; set; }
        public decimal OrderTotal { get; set; }
    }
}