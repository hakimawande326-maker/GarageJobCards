namespace GarageJobCards.Infrastructure
{
    public static class EmailPolicy
    {
        public const string RequirementsText = "Email must be a Gmail address (ending in @gmail.com).";

        // Customers use their own Gmail address. Staff use the shop's own
        // domain instead - two different rules for two different account
        // types, not one universal one.
        public const string StaffRequirementsText = "Email must be a philasauto.co.za address (ending in @philasauto.co.za).";

        public static bool IsAllowedDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return email.Trim().ToLowerInvariant().EndsWith("@gmail.com");
        }

        public static bool IsAllowedStaffDomain(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return email.Trim().ToLowerInvariant().EndsWith("@philasauto.co.za");
        }
    }
}
