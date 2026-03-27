using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class Users
    {
        [Required(ErrorMessage = "Full Name is requiered!")]
        [StringLength(100)]
        [DisplayName("Full Name")]
        public string FullName { get; set; }


        [Required]
        [StringLength(100)]
        [EmailAddress]
        [DisplayName("Email adress requiered!")]
        public string Email { get; set; }

        [MaxLength(18, ErrorMessage = "Max password length is 18 characters")]
        [MinLength(8, ErrorMessage = "Minimum length must be 8 characters long")]
        [Required(ErrorMessage = "Password is requiered")]
        [DisplayName("Password 8-18 characters long")]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}