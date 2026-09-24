using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        public int OwnerId { get; set; } // FK -> User.Id (a Customer). A customer can own many vehicles.
        public virtual User Owner { get; set; }

        [Required(ErrorMessage = "Make is required.")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Make must be between 2 and 40 characters.")]
        [Display(Name = "Make")]
        public string Make { get; set; }

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(40, MinimumLength = 1, ErrorMessage = "Model must be between 1 and 40 characters.")]
        [Display(Name = "Model")]
        public string Model { get; set; }

        [StringLength(30)]
        [Display(Name = "Colour")]
        public string Colour { get; set; }

        [StringLength(17, MinimumLength = 5, ErrorMessage = "VIN should be 5-17 characters.")]
        [Display(Name = "VIN (Vehicle Identification Number)")]
        public string VinNumber { get; set; }

        [Required(ErrorMessage = "Plate number is required.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Plate number must be between 4 and 20 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-]+$", ErrorMessage = "Plate number can only contain letters, numbers, spaces, and hyphens.")]
        [Display(Name = "Plate number")]
        public string PlateNumber { get; set; }

        [Range(0, 999999, ErrorMessage = "Mileage must be between 0 and 999,999 km.")]
        [Display(Name = "Mileage (km)")]
        public int? Mileage { get; set; }
    }
}
