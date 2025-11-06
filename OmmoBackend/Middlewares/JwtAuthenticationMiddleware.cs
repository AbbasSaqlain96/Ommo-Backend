using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OmmoBackend.Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OmmoBackend.Middlewares
{
    public class JwtAuthenticationMiddleware
    {

        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger, IConfiguration configuration)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentException(nameof(configuration));
        }

        private static readonly List<(string Path, string Module, string Component, int AccessLevel)> ApiPermissions = new()
        {
            ("/api/role/delete-role", "Setting", "Role", 2),
            ("/api/role/get-roles", "Setting", "Role", 1),
            ("/api/user/create-user", "Setting", "User", 2),
            ("/api/units/get-unit-info", "Unit", null, 1),
            ("/api/user/get-user-info", "Setting", "User", 1),
            ("/api/truck/get-truck-info", null, null, 1),
            ("/api/trailer/get-trailer-info", null, null, 1),
            ("/api/tab/get-tabs", null, null, 1),
            ("/api/issue-ticket/create-issue-ticket", null, null, 2),
            ("/api/issue-ticket/get-issue-ticket", null, null, 1),
            ("/api/user/update-user", "Setting", "User", 2),
            ("/api/role/create-role", "Setting", "Role", 2),
            ("/api/user/toggle-status", "Setting", "User", 2),
            ("/api/user/get-user-by-company", "Setting", "User", 1),
            ("/api/user/update-myself", "Setting", "User", 2),
            ("/api/company/get-company-profile", "Setting", "General", 1),
            ("/api/company/update-company-profile", "Setting", "General", 2),
            ("/api/driver/get-driver-list", "Safety", "Driver", 1),
            ("/api/driver/detail", "Safety", "Driver", 1),
            ("/api/driver/documents", "Safety", "Driver", 1),
            ("/api/driver/hire-driver","Safety", "Driver", 2),
            ("/api/accident/details", "Safety", null, 1),
            ("/api/incident/details", "Safety", null, 1),
            ("/api/ticket/details","Safety", null, 1),
            ("/api/ticket/create","Safety", "Oversight", 2),
            ("/api/incident/create","Safety", "Oversight", 2),
            ("/api/accident/create","Safety", "Oversight", 2),
            ("/api/events/get-events","Safety", "", 1),
            //("/api/events/get-events-by-driver", "Safety", "Driver", 2),
            ("/api/driver/performance", "Safety", "Driver", 1),
            ("/api/performance/get-performance","Safety", "Oversight", 1),
            ("/api/asset/get-assets","Shop", "Assets", 1),
            ("/api/asset/get-asset-details","Shop", "Assets", 1),
            ("/api/asset/shop-history","Shop", "Assets", 1),
            ("/api/asset/add-asset","Shop", "Assets", 2),
            ("/api/category/get-maintenance-category", "Shop", "Category", 1),
            ("/api/category/delete", "Shop", "Category", 2),
            ("/api/category/create", "Shop", "Category", 2),
            ("/api/issue-ticket/get", "Shop", "Tickets", 1),
            ("/api/issue-ticket/create", "Shop", "Tickets", 2),
            ("/api/issue-ticket/update", "Shop", "Tickets", 2),
            ("/api/driver/get-driver-list", "Safety", "Oversight", 2),
            ("/api/category/get-maintenance-category", "Shop", "Tickets", 2),
            ("/api/asset/get-assets","Shop", "Tickets", 2),
            ("/api/user/get-user-by-company", "Shop", "Tickets", 2),
            ("/api/incident/update","Safety", "Oversight", 2),
            ("/api/ticket/update","Safety", "Oversight", 2),
            ("/api/accident/update","Safety", "Oversight", 2),
            ("/api/dot-inspection/create", "Safety", "Oversight", 2),
            ("/api/dot-inspection/details", "Safety", "Oversight", 1),
            ("/api/dot-inspection/update", "Safety", "Oversight", 2),
            ("/api/warning/create", "Safety", "Oversight", 2),
            ("/api/warning/details", "Safety", "Oversight", 1),
            ("/api/warning/update", "Safety", "Oversight", 2),
            ("/api/user/change-password", "Setting", "User", 2),
            ("/api/integration/get-integrations", "Setting", "Integration", 1),
            ("/api/integration/default-integration", "Setting", "Integration", 1),
            ("/api/loadboard/get-loads", "Load Board", "LoadBoard", 1),
            ("/api/integration/send-integration-request", "Setting", "Integration", 2),
            ("/api/call/get-called-loads", "Load Board", "Calls", 1),
            ("/api/aiagent/outbound", "Load Board", "LoadBoard", 1),
            ("/api/integration/toggle-integration-status", "Setting", "Integration", 1)
        };

        private string JwtSecretKey => _configuration["Jwt:Key"];

        public async Task InvokeAsync(HttpContext context, IRoleModuleService roleModuleService)
        {
            try
            {
                //     // Check if the endpoint allows anonymous access
                if (context.Request.Method == "OPTIONS")
                {
                    await _next(context); // Skip authentication
                    return;
                }
                var endpoint = context.GetEndpoint();
                if (endpoint?.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
                {
                    await _next(context); // Skip authentication
                    return;
                }

                // Extract the Authorization header
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Authorization token is missing.");
                    return;
                }

                // Validate and decode the JWT token
                var jwtToken = ValidateJwtToken(token);
                if (jwtToken == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid or expired token.");
                    return;
                }

                // Extract claims
                var companyId = int.Parse(jwtToken.Claims.First(c => c.Type == "Company_ID").Value);
                var roleId = int.Parse(jwtToken.Claims.First(c => c.Type == "Role_ID").Value);
                var path = context.Request.Path.Value?.ToLower();

                if (endpoint?.Metadata.GetMetadata<RequireAuthenticationOnlyAttribute>() != null)
                {
                    // Token is already validated; skip module/component checks
                    await _next(context);
                    return;
                }

                // Find the required module and access level for the API
                //var apiPermission = ApiPermissions.FirstOrDefault(ap => ap.Path.ToLower() == path);

                //if (apiPermission == default)
                //{
                //    context.Response.StatusCode = StatusCodes.Status404NotFound;
                //    await context.Response.WriteAsync("API path is not recognized.");
                //    return;
                //}
                var apiPermissions = ApiPermissions.Where(ap => ap.Path.ToLower() == path).ToList();

                if (!apiPermissions.Any())
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.Response.WriteAsync("API path is not recognized.");
                    return;
                }


                //if (apiPermission.Path != "/api/user/get-user-by-company")
                //{

                //    // Check module access
                //    var hasModuleAccess = await roleModuleService.HasAccessAsync(roleId, apiPermission.Module, apiPermission.AccessLevel);
                //    if (!hasModuleAccess)
                //    {
                //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //        await context.Response.WriteAsync("Access denied. Insufficient module permissions.");
                //        return;
                //    }

                //    // Check component access if applicable
                //    if (!string.IsNullOrEmpty(apiPermission.Component))
                //    {
                //        var hasComponentAccess = await roleModuleService.HasComponentAccessAsync(roleId, apiPermission.Component, apiPermission.AccessLevel);
                //        if (!hasComponentAccess)
                //        {
                //            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //            await context.Response.WriteAsync("Access denied. Insufficient component permissions.");
                //            return;
                //        }
                //    }
                //}
                bool hasAccess = false;
                foreach (var permission in apiPermissions)
                {
                    var moduleAccess = await roleModuleService.HasAccessAsync(roleId, permission.Module, permission.AccessLevel);

                    if (!string.IsNullOrEmpty(permission.Component))
                    {
                        var componentAccess = await roleModuleService.HasComponentAccessAsync(roleId, permission.Component, permission.AccessLevel);
                        if (moduleAccess && componentAccess)
                        {
                            hasAccess = true;
                            break; // No need to check further, user has access
                        }
                    }
                    else if (moduleAccess) // If no component is defined, module-level access is sufficient
                    {
                        hasAccess = true;
                        break;
                    }
                }

                if (!hasAccess)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Access denied. Insufficient permissions.");
                    return;
                }

                // Proceed to the next middleware/component
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in JWT Authentication Middleware.");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred during authentication.");
            }
        }

        private JwtSecurityToken? ValidateJwtToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(JwtSecretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                return (JwtSecurityToken)validatedToken;
            }
            catch
            {
                return null;
            }
        }
    }
}