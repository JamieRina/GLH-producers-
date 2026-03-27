using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class ProductModel
    {
        public int ProductId { get; set; }

        [Required]
        public int ProducerId { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [Range(0, 9999.99, ErrorMessage = "Price must be between 0 and 9999.99.")]
        public decimal Price { get; set; }

        [Range(0, 99999, ErrorMessage = "Stock cannot be negative.")]
        public int Stock { get; set; }

        [StringLength(50)]
        public string BatchCode { get; set; }

        public bool IsAvailable { get; set; }
    }
}