using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GarageJobCards.Models
{
    public enum UserRole
    {
        Receptionist = 0,
        Manager = 1,
        Mechanic = 2,
        Customer = 3
    }

    public class User
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required, StringLength(120)]
        [Display(Name = "Email (used to log in)")]
        public string Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone number")]
        public string Phone { get; set; }

        public UserRole Role { get; set; }

        // Never store plain-text passwords - see Infrastructure/PasswordHelper.cs
        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        // Set when a password reset is requested; cleared once used or expired.
        public string PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiryUtc { get; set; }

        [NotMapped]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } // used only on the registration form, never persisted directly

        public User()
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
