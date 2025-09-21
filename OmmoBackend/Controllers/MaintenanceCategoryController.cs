using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Constants;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Services.Interfaces;
using System.Security.Claims;
using Twilio.Http;

namespace OmmoBackend.Controllers
{
    [ApiController]
    [Route("api/category")]
    public class MaintenanceCategoryController : Controller
    {
        private readonly IMaintenanceCategoryService _maintenanceCategoryService;
        private readonly ILogger<MaintenanceCategoryController> _logger;
        public MaintenanceCategoryController(IMaintenanceCategoryService maintenanceCategoryService, ILogger<MaintenanceCategoryController> logger)
        {
            _maintenanceCategoryService = maintenanceCategoryService;
            _logger = logger;
        }

        [HttpGet("get-maintenance-category")]
        [Authorize]
        public async Task<IActionResult> GetCategories([FromQuery] string? catType, [FromQuery] int? carrierId)
        {
            try
            {
                // Extract CompanyId from token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");

                _logger.LogInformation("Fetching maintenance categories for Company ID: {CompanyId}, CatType: {CatType}, CarrierId: {CarrierId}", companyId, catType, carrierId);

                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                var response = await _maintenanceCategoryService.GetCategoriesAsync(catType, carrierId, companyId);
                
                if (!response.Success)
                {
                    _logger.LogWarning("Failed to fetch maintenance categories: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Successfully fetched maintenance categories for Company ID: {CompanyId}", companyId);
                return ApiResponse.Success(response.Data, "Categories fetched successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service unavailable while fetching maintenance categories.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpDelete("delete")]
        [Authorize]
        public async Task<IActionResult> DeleteCategory([FromQuery] int categoryId)
        {
            try
            {
                // Extract CompanyId from token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");

                _logger.LogInformation("Deleting maintenance category. CategoryId: {CategoryId}, CompanyId: {CompanyId}", categoryId, companyId);

                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                var response = await _maintenanceCategoryService.DeleteCategoryAsync(categoryId, companyId);

                if (!response.Success)
                {
                    _logger.LogWarning("Delete failed: {ErrorMessage}", response.ErrorMessage);
                    return ApiResponse.Error(response.ErrorMessage, response.StatusCode);
                }

                _logger.LogInformation("Category {CategoryId} deleted successfully for CompanyId {CompanyId}", categoryId, companyId);
                return ApiResponse.Success(null, response.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting category {CategoryId}", categoryId);
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> AddCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                // Extract CompanyId from token
                int companyId = int.Parse(User.Claims.FirstOrDefault(c => c.Type == "Company_ID")?.Value ?? "0");

                _logger.LogInformation("Creating new maintenance category for CompanyId {CompanyId}", companyId);

                if (companyId <= 0)
                {
                    _logger.LogWarning("Invalid Company ID: {CompanyId}", companyId);
                    return ApiResponse.Error("Invalid Company ID.", 400);
                }

                var result = await _maintenanceCategoryService.CreateCategoryAsync(request, companyId);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create category: {ErrorMessage}", result.ErrorMessage);
                    return ApiResponse.Error(result.ErrorMessage, result.StatusCode);
                }

                _logger.LogInformation("Category created successfully for CompanyId {CompanyId}", companyId);

                return ApiResponse.Success(null, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a category.");
                return ApiResponse.Error(ErrorMessages.ServerDown, 503);
            }
        }
    }
}