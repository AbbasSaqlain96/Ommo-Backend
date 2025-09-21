using Newtonsoft.Json;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Models;

namespace OmmoBackend.Parsers
{
    public static class ClaimParser
    {
        public static List<Claims> DeserializeClaims(string claimInfoJson, ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(claimInfoJson))
                return new List<Claims>();

            try
            {
                if (!claimInfoJson.TrimStart().StartsWith("["))
                    claimInfoJson = "[" + claimInfoJson + "]";

                var rawClaims = DeserializeRawClaims(claimInfoJson);

                return rawClaims
                    .Select(MapToEntity)
                    .ToList();
            }
            catch (JsonException ex)
            {
                logger?.LogError(ex, "Failed to deserialize claim info JSON.");
                throw new ArgumentException("Invalid ClaimInfo JSON format.", ex);
            }
        }

        private static List<ClaimInputDto> DeserializeRawClaims(string claimInfoJson)
        {
            return JsonConvert.DeserializeObject<List<ClaimInputDto>>(claimInfoJson)
                   ?? new List<ClaimInputDto>();
        }

        private static Claims MapToEntity(ClaimInputDto dto)
        {
            var claimType = ClaimMapper.MapClaimType(dto.claim_type);
            var claimStatus = ClaimMapper.MapClaimStatus(dto.status);

            return new Claims
            {
                claim_type = claimType,
                status = claimStatus,
                claim_description = dto.claim_description,
                claim_amount = dto.claim_amount,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };
        }
    }
}
