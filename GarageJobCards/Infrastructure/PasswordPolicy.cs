using System.Text.RegularExpressions;

namespace GarageJobCards.Infrastructure
{
    public static class PasswordPolicy
    {
        public const string RequirementsText =
            "At least 8 characters, with an uppercase letter, a lowercase letter, a number, and a special character (e.g. ! @ # $ %).";

        public static bool IsStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            if (!Regex.IsMatch(password, "[A-Z]")) return false;      // uppercase
            if (!Regex.IsMatch(password, "[a-z]")) return false;      // lowercase
            if (!Regex.IsMatch(password, "[0-9]")) return false;      // digit
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) return false; // special character
            return true;
        }
    }
}
