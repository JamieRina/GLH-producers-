using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class ProducerModel
    {
        public int ProducerId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Business name is required.")]
        [StringLength(100)]
        public string BusinessName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        [StringLength(255)]
        public string Methods { get; set; }
        
    }
}