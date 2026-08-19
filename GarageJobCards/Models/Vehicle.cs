using System.ComponentModel.DataAnnotations;

namespace GarageJobCards.Models
{
 public class Vehicle
 {
 public int Id { get; set; }

 public int OwnerId { get; set; } // FK -> User.Id (a Customer)
 public virtual User Owner { get; set; }

 [Required, StringLength(40)]
 [Display(Name = "Make")]
 public string Make { get; set; }

 [Required, StringLength(40)]
 [Display(Name = "Model")]
 public string Model { get; set; }

 [Required, StringLength(20)]
 [Display(Name = "Plate number")]
 public string PlateNumber { get; set; }

 [Display(Name = "Mileage (km)")]
 public int? Mileage { get; set; }
 }
}
