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

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 80 characters.")]
        [RegularExpression(@"^[a-zA-Z\s'\-]+$", ErrorMessage = "Full name can only contain letters, spaces, hyphens, and apostrophes.")]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(120)]
        [EmailAddress(ErrorMessage = "Enter a valid email address (e.g. name@gmail.com).")]
        [Display(Name = "Email (used to log in)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits (e.g. 0721234567).")]
        [Display(Name = "Phone number")]
        public string Phone { get; set; }

        [StringLength(200, MinimumLength = 5, ErrorMessage = "Street address must be at least 5 characters.")]
        [Display(Name = "Street address")]
        public string Address { get; set; }

        [StringLength(80)]
        [RegularExpression(@"^[a-zA-Z\s'\-]*$", ErrorMessage = "City can only contain letters, spaces, and hyphens.")]
        [Display(Name = "City / Township")]
        public string City { get; set; }

        [RegularExpression(@"^\d{4}$", ErrorMessage = "Postal code must be exactly 4 digits (e.g. 4066).")]
        [Display(Name = "Postal code")]
        public string PostalCode { get; set; }

        public UserRole Role { get; set; }

        // Never store plain-text passwords - see Infrastructure/PasswordHelper.cs
        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string PasswordSalt { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }

        // Deactivated accounts can't log in, but stay in the database so job
        // card history (which references them) stays intact.
        public bool IsActive { get; set; }

        // Base64 PNG data URL of a drawn signature - only meaningful for
        // Manager accounts, used to sign off job cards at pickup.
        public string SignatureImageDataUrl { get; set; }

        // One-time code sent via SMS for password reset - valid for a short
        // window (1 minute 50 seconds), cleared once used or expired.
        public string OtpCode { get; set; }
        public DateTime? OtpExpiryUtc { get; set; }

        [NotMapped]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } // used only on the registration form, never persisted directly

        public User()
        {
            CreatedAtUtc = DateTime.UtcNow;
            IsActive = true;
        }
    }
}