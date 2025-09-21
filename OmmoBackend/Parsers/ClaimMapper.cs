using OmmoBackend.Helpers.Enums;

namespace OmmoBackend.Parsers
{
    public class ClaimMapper
    {
        public static ClaimType MapClaimType(string? claimTypeStr)
        {
            if (!Enum.TryParse<ClaimType>(claimTypeStr, true, out var claimType))
                throw new ArgumentException($"Invalid claim type: {claimTypeStr}");

            return claimType;
        }

        public static ClaimStatus MapClaimStatus(string? statusStr)
        {
            if (!Enum.TryParse<ClaimStatus>(statusStr, true, out var claimStatus))
                throw new ArgumentException($"Invalid claim status: {statusStr}");

            return claimStatus;
        }
    }
}
