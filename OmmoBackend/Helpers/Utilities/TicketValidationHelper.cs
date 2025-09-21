namespace OmmoBackend.Helpers.Utilities
{
    public static class TicketValidationHelper
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "new", "closed", "in court"
        };

        public static bool IsValidStatus(string status)
        {
            return !string.IsNullOrWhiteSpace(status) && AllowedStatuses.Contains(status.Trim());
        }

        public static string GetAllowedStatusesString()
        {
            return string.Join(", ", AllowedStatuses);
        }
    }
}
