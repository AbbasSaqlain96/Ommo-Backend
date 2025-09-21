using System.Text.RegularExpressions;

namespace OmmoBackend.Parsers
{
    public static class IntegrationEmailParser
    {  
        // Patterns are permissive; tweak if vendors send different keys/labels
        private static readonly Regex TruckstopSuccessRegex = new Regex(
            @"IntegrationID:\s*(?<integrationId>\d+).*?API Username:\s*(?<username>\S+).*?API Password:\s*(?<password>\S+).*?Customer:\s*(?<customer>.+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex TruckstopRejectRegex = new Regex(
            @"Reason:\s*(?<reason>.+?)\s*Customer:\s*(?<customer>.+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DATSuccessRegex = new Regex(
            @"Service Account Email:\s*(?<serviceEmail>\S+).*?Temporary Password:\s*(?<password>\S+).*?Customer:\s*(?<customer>.+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DATRejectRegex = new Regex(
            @"Reason:\s*(?<reason>.+?)\s*Customer:\s*(?<customer>.+)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public class ParsedResult
        {
            public string Provider { get; set; } = null!; // "Truckstop" or "DAT"
            public bool Success { get; set; }
            public Dictionary<string, string> Fields { get; set; } = new();
            public string RawText { get; set; } = "";
        }

        public static ParsedResult? Parse(string subject, string body)
        {
            subject = subject ?? "";
            body = body ?? "";
            var result = new ParsedResult { RawText = body };

            if (subject.IndexOf("Truckstop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Provider = "Truckstop";
                if (subject.IndexOf("SUCCESS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var m = TruckstopSuccessRegex.Match(body);
                    if (!m.Success) return null;
                    result.Success = true;
                    result.Fields["IntegrationID"] = m.Groups["integrationId"].Value;
                    result.Fields["Username"] = m.Groups["username"].Value;
                    result.Fields["Password"] = m.Groups["password"].Value;
                    result.Fields["Customer"] = m.Groups["customer"].Value.Trim();
                }
                else
                {
                    var m = TruckstopRejectRegex.Match(body);
                    if (!m.Success) return null;
                    result.Success = false;
                    result.Fields["Reason"] = m.Groups["reason"].Value.Trim();
                    result.Fields["Customer"] = m.Groups["customer"].Value.Trim();
                }
                return result;
            }
            else if (subject.IndexOf("DAT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Provider = "DAT";
                if (subject.IndexOf("SUCCESS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var m = DATSuccessRegex.Match(body);
                    if (!m.Success) return null;
                    result.Success = true;
                    result.Fields["ServiceAccountEmail"] = m.Groups["serviceEmail"].Value.Trim();
                    result.Fields["Password"] = m.Groups["password"].Value.Trim();
                    result.Fields["Customer"] = m.Groups["customer"].Value.Trim();
                }
                else
                {
                    var m = DATRejectRegex.Match(body);
                    if (!m.Success) return null;
                    result.Success = false;
                    result.Fields["Reason"] = m.Groups["reason"].Value.Trim();
                    result.Fields["Customer"] = m.Groups["customer"].Value.Trim();
                }
                return result;
            }

            // Unknown provider
            return null;
        }
    }
}
