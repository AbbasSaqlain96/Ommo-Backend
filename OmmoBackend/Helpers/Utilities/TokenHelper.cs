using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using System;
using System.Linq;
using System.Security.Claims;

namespace OmmoBackend.Helpers.Utilities
{
    public static class TokenHelper
    {
        /// <summary>
        /// Extracts the Company_ID from the claims of the authenticated user.
        /// </summary>
        /// <param name="userClaims">The ClaimsPrincipal of the authenticated user.</param>
        /// <returns>The Company_ID as an integer. Returns 0 if not found or invalid.</returns>
        public static int GetCompanyId(ClaimsPrincipal userClaims)
        {
            if (userClaims == null)
                throw new ArgumentNullException(nameof(userClaims), "User claims cannot be null.");

            var companyIdClaim = userClaims.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value;

            return int.TryParse(companyIdClaim, out var companyId) ? companyId : 0;
        }

        public static bool TryGetCompanyId(ClaimsPrincipal user, ILogger logger, out int companyId, out IActionResult? errorResponse)
        {
            companyId = 0;
            errorResponse = null;

            var companyIdValue = user?.Claims?.FirstOrDefault(c => c.Type == "Company_ID")?.Value;

            if (!int.TryParse(companyIdValue, out companyId) || companyId <= 0)
            {
                logger.LogWarning("Unauthorized access: Missing or invalid Company ID in token.");
                errorResponse = ApiResponse.Error("You do not have permission to access this resource.", 401);
                return false;
            }

            return true;
        }

        public static int GetUserIdFromClaims(ClaimsPrincipal user)
        {
            if (user == null)
                return 0;

            // First try with custom claim type (e.g., "user_id"), then fallback to NameIdentifier
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type == "user_id")
                              ?? user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
                return 0;

            return int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
        }
    }
}

