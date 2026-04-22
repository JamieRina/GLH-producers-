using System.Collections.Generic;

namespace GLH_producers.Models
{
    public class HomeIndexViewModel
    {
        public HomeIndexViewModel()
        {
            FeaturedProducts = new List<ProductModel>();
        }

        public List<ProductModel> FeaturedProducts { get; set; }
    }
}
