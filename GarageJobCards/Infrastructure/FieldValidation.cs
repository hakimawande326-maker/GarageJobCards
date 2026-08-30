using System.Text.RegularExpressions;

namespace GarageJobCards.Infrastructure
{
    public static class FieldValidation
    {
        public const string PhoneRequirementsText = "Phone number must be exactly 10 digits (e.g. 0721234567).";
        public const string PostalCodeRequirementsText = "Postal code must be exactly 4 digits (e.g. 4066).";
        public const string NameRequirementsText = "Name must be 2-80 characters and contain only letters, spaces, hyphens, and apostrophes.";

        public static bool IsValidPhone(string phone)
        {
            return !string.IsNullOrEmpty(phone) && Regex.IsMatch(phone, @"^\d{10}$");
        }

        public static bool IsValidPostalCode(string postalCode)
        {
            // Optional field - blank is fine, but if provided it must be 4 digits.
            if (string.IsNullOrWhiteSpace(postalCode)) return true;
            return Regex.IsMatch(postalCode, @"^\d{4}$");
        }

        public static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var trimmed = name.Trim();
            return trimmed.Length >= 2 && trimmed.Length <= 80 && Regex.IsMatch(trimmed, @"^[a-zA-Z\s'\-]+$");
        }
    }
}
