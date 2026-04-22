using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GLH_producers.Models
{
    public class CheckoutViewModel : IValidatableObject
    {
        public CheckoutViewModel()
        {
            BasketItems = new List<BasketItem>();
            OrderType = "Collection";
            RequiredDate = DateTime.Today;
        }

        [Required(ErrorMessage = "Choose collection or delivery.")]
        public string OrderType { get; set; }

        [StringLength(255)]
        public string Address { get; set; }

        [Required(ErrorMessage = "Required date is required.")]
        [DataType(DataType.Date)]
        public DateTime? RequiredDate { get; set; }

        public List<BasketItem> BasketItems { get; set; }
        public decimal Total { get; set; }

        public int PointsEarned
        {
            get { return (int)Math.Floor(Total); }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string[] validTypes = { "Collection", "Delivery" };

            if (!validTypes.Contains(OrderType))
            {
                yield return new ValidationResult("Order type must be Collection or Delivery.", new[] { "OrderType" });
            }

            if (OrderType == "Delivery" && string.IsNullOrWhiteSpace(Address))
            {
                yield return new ValidationResult("Address is required for delivery.", new[] { "Address" });
            }

            if (RequiredDate.HasValue && RequiredDate.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult("Required date cannot be in the past.", new[] { "RequiredDate" });
            }
        }
    }
}
