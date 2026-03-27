using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class Registeration
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is requiered")]
        [DisplayName("Password 8-18 characters long")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least  characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password, ErrorMessage = "Password do not match")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}