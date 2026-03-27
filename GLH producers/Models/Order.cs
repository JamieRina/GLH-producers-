using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace GLH_producers.Models
{
    public class Order : IValidatableObject
    {
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime OrderDate { get; set; }

        [Required(ErrorMessage = "Order type is required.")]
        [StringLength(20)]
        public string OrderType { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RequiredDate { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [StringLength(20)]
        public string Status { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Total must be 0 or more.")]
        public decimal Total { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string[] validOrderTypes = { "Collection", "Delivery" };
            if (!validOrderTypes.Contains(OrderType))
            {
                yield return new ValidationResult(
                    "Order type must be Collection or Delivery.",
                    new[] { nameof(OrderType) });
            }

            if (OrderType == "Delivery" && string.IsNullOrWhiteSpace(Address))
            {
                yield return new ValidationResult(
                    "Address is required for delivery orders.",
                    new[] { nameof(Address) });
            }

            if (RequiredDate.HasValue && RequiredDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Required date cannot be in the past.",
                    new[] { nameof(RequiredDate) });
            }

            string[] validStatuses = { "Pending", "Confirmed", "Completed", "Cancelled" };
            if (!validStatuses.Contains(Status))
            {
                yield return new ValidationResult(
                    "Invalid order status.",
                    new[] { nameof(Status) });
            }
        }
    }
}