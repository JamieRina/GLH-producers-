using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class ProducerDashboardViewModel
    {
        public ProducerDashboardViewModel()
        {
            Products = new List<ProductModel>();
            Orders = new List<ProducerOrderRowViewModel>();
        }

        public ProducerModel Producer { get; set; }
        public List<ProductModel> Products { get; set; }
        public List<ProducerOrderRowViewModel> Orders { get; set; }




    }
}