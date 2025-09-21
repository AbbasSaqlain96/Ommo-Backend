using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Services.Implementations
{
    public class ModuleService : IModuleService
    {
        private readonly IModuleRepository _moduleRepository;
        private readonly ILogger<ModuleService> _logger;

        public ModuleService(IModuleRepository moduleRepository, ILogger<ModuleService> logger)
        {
            _moduleRepository = moduleRepository;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a module with the specified Id exists.
        /// </summary>
        /// <param name="moduleId">The Id of the module to check.</param>
        /// <returns>True if the module exists; otherwise, false.</returns>
        public async Task<bool> ModuleExists(int moduleId)
        {
            try
            {
                _logger.LogInformation("Checking if module with ID {ModuleId} exists.", moduleId);

                var module = await _moduleRepository.GetByIdAsync(moduleId);
                if (module == null)
                {
                    _logger.LogWarning("Module with ID {ModuleId} not found.", moduleId);
                    return false;
                }

                _logger.LogInformation("Module with ID {ModuleId} exists.", moduleId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking if module with ID {ModuleId} exists.", moduleId);
                throw;
            }

        }

        public async Task<ServiceResponse<List<ModuleDto>>> GetModulesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all modules with components.");
                var modules = await _moduleRepository.GetModulesWithComponentsAsync();

                _logger.LogInformation("Successfully retrieved {ModuleCount} modules.", modules.Count);
                return ServiceResponse<List<ModuleDto>>.SuccessResponse(modules, "Modules retrieved successfully.");
            }
            catch (UnauthorizedAccessException uaEx)
            {
                _logger.LogWarning(uaEx, "Unauthorized access while fetching modules.");
                return ServiceResponse<List<ModuleDto>>.ErrorResponse("You do not have permission to access this resource", 401);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error occurred while fetching modules.");
                return ServiceResponse<List<ModuleDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}