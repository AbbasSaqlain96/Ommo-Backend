using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class TabService : ITabService
    {
        private readonly ITabRepository _tabRepository;
        private readonly ILogger<TabService> _logger;

        public TabService(ITabRepository tabRepository, ILogger<TabService> logger)
        {
            _tabRepository = tabRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<TabsResponseDto>> GetTabsAsync(int roleId)
        {
            _logger.LogInformation("Fetching tabs for Role ID: {RoleId}", roleId);

            try
            {
                // Fetch the modules and components for the given Role_ID
                var modulesWithComponents = await _tabRepository.GetModulesWithComponentsByRoleAsync(roleId);

                if (modulesWithComponents is null || !modulesWithComponents.Any())
                {
                    _logger.LogWarning("No tabs found for Role ID: {RoleId}", roleId);
                    return ServiceResponse<TabsResponseDto>.ErrorResponse("No tabs found for the given Role_ID.", 404);
                }

                // Group and format the data
                var formattedTabs = modulesWithComponents
                    .GroupBy(m => new { m.ModuleName, m.AccessLevel })
                    .Select(group => new TabsDto
                    {
                        ModuleName = group.Key.ModuleName,
                        // Safely parse the string to AccessLevel enum and then cast it to int
                        AccessLevel = Enum.TryParse(group.Key.AccessLevel, out AccessLevel accessLevel)
                            ? (int)accessLevel  // Cast AccessLevel to int
                            : (int)AccessLevel.read_only, // Default to ReadOnly, cast to int

                        Components = group
                            //.Where(c => !string.IsNullOrEmpty(c.ComponentName)) // Ensure valid components
                            .Where(c => !string.IsNullOrEmpty(c.ComponentName) && c.ComponentAccessLevel > 0)
                            .Select(c => new ComponentDto
                            {
                                ComponentName = c.ComponentName,
                                AccessLevel = Enum.TryParse(c.ComponentAccessLevel.ToString(), out AccessLevel componentAccessLevel)
                                    ? (int)componentAccessLevel // Convert AccessLevel to int
                                    : (int)AccessLevel.read_only // Default to ReadOnly if parsing fails
                            })
                            .ToList()
                    })
                    .Where(tab => tab.Components.Any() || tab.AccessLevel > 0) // Ensure only modules with valid components are included
                    .ToList();

                _logger.LogInformation("Successfully fetched {Count} tabs for Role ID: {RoleId}", formattedTabs.Count, roleId);

                return ServiceResponse<TabsResponseDto>.SuccessResponse(new TabsResponseDto { Tabs = formattedTabs }, "Tabs retrieved successfully.");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Unauthorized access while fetching tabs for Role ID: {RoleId}", roleId);
                return ServiceResponse<TabsResponseDto>.ErrorResponse("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching or processing tabs for Role ID: {RoleId}", roleId);
                return ServiceResponse<TabsResponseDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }



        // Helper method to safely parse string values to enums
        private static TEnum TryParseEnum<TEnum>(string value) where TEnum : struct
        {
            if (Enum.TryParse(value, out TEnum result))
            {
                return result;
            }
            else
            {
                // Return the default value of the enum (e.g., ReadOnly)
                return default(TEnum);
            }
        }
        // public async Task<ServiceResponse<TabsResponseDto>> GetTabsAsync()
        // {
        //     try
        //     {
        //         // Fetch the modules with components
        //         var modulesWithComponents = await _tabRepository.GetModulesWithComponentsAsync();

        //         if (modulesWithComponents is null)
        //             return ServiceResponse<TabsResponseDto>.ErrorResponse("No tabs found.");

        //         // Format the data
        //         var groupTabs = modulesWithComponents
        //             .GroupBy(m => m.ModuleName)
        //             .ToDictionary(
        //                 g => g.Key,
        //                 g => g.Select(c => c.ComponentName).ToList()
        //             );

        //         return ServiceResponse<TabsResponseDto>.SuccessResponse(new TabsResponseDto
        //         {
        //             Tabs = groupTabs
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         throw new ApplicationException("An error occurred while fetching or processing tabs.");
        //     }
        // }
    }
}