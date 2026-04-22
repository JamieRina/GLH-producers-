using System;
using System.Collections.Generic;

namespace GLH_producers.Models
{
    public class CustomerOrderViewModel
    {
        public CustomerOrderViewModel()
        {
            Items = new List<OrderItemViewModel>();
        }

        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderType { get; set; }
        public string Address { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string Status { get; set; }
        public decimal Total { get; set; }
        public List<OrderItemViewModel> Items { get; set; }
    }
}
