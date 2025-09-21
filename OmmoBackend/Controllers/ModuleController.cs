using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Middlewares;
using OmmoBackend.Services.Interfaces;

namespace OmmoBackend.Controllers
{
    [Route("api/module")]
    [ApiController]
    public class ModuleController : ControllerBase
    {
        private readonly IModuleService _moduleService;
        private readonly ILogger<ModuleController> _logger;
        /// <summary>
        /// Initializes a new instance of the ModuleController class with the specified module service.
        /// </summary>
        public ModuleController(IModuleService moduleService, ILogger<ModuleController> logger)
        {
            _moduleService = moduleService;
            _logger = logger;
        }

        [HttpGet]
        [Route("modules")]
        [AllowAnonymous]
        public async Task<IActionResult> GetModules()
        {
            _logger.LogInformation("Received request to fetch modules.");

            try
            {
                var serviceResponse = await _moduleService.GetModulesAsync();

                if (serviceResponse.Success)
                {
                    return ApiResponse.Success(serviceResponse.Data, "Modules retrieved successfully.");
                }

                _logger.LogWarning("Failed to retrieve modules: {Message}", serviceResponse.ErrorMessage);
                return ApiResponse.Error(serviceResponse.ErrorMessage, serviceResponse.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server error occurred while fetching modules.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}
