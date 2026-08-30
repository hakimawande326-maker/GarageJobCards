namespace GarageJobCards.Infrastructure
{
    public static class EmailPolicy
    {
        public const string RequirementsText = "Email must be a Gmail address (ending in @gmail.com).";

        public static bool IsAllowedDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return email.Trim().ToLowerInvariant().EndsWith("@gmail.com");
        }
    }
}
