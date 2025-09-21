
using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Enums;
using OmmoBackend.Helpers.Responses;
using OmmoBackend.Models;
using OmmoBackend.Repositories.Implementations;
using OmmoBackend.Repositories.Interfaces;
using OmmoBackend.Services.Interfaces;
using System.Net;

namespace OmmoBackend.Services.Implementations
{
    public class MaintenanceCategoryService : IMaintenanceCategoryService
    {
        private readonly ICarrierRepository _carrierRepository;
        private readonly IMaintenanceCategoryRepository _maintenanceCategoryRepository;
        private readonly IIssueTicketRepository _issueTicketRepository;
        private readonly ILogger<MaintenanceCategoryService> _logger;

        public MaintenanceCategoryService(ICarrierRepository carrierRepository, IMaintenanceCategoryRepository maintenanceCategoryRepository, IIssueTicketRepository issueTicketRepository, ILogger<MaintenanceCategoryService> logger)
        {
            _carrierRepository = carrierRepository;
            _maintenanceCategoryRepository = maintenanceCategoryRepository;
            _issueTicketRepository = issueTicketRepository;
            _logger = logger;
        }

        public async Task<ServiceResponse<List<CategoryResponseDto>>> GetCategoriesAsync(string? catType, int? carrierId, int companyId)
        {
            try
            {
                _logger.LogInformation("Fetching categories. CatType: {CatType}, CarrierId: {CarrierId}, CompanyId: {CompanyId}", catType, carrierId, companyId);

                if (carrierId == null)
                {
                    _logger.LogInformation("CarrierId is null, retrieving it using companyId: {CompanyId}", companyId);
                    // Get authenticated user's carrier_id
                    carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);
                }

                // Fetch categories based on filtering conditions
                var categories = await _maintenanceCategoryRepository.GetCategoriesAsync(catType, carrierId);

                if (categories == null || !categories.Any())
                {
                    _logger.LogWarning("No categories found for CatType: {CatType}, CarrierId: {CarrierId}", catType, carrierId);
                    return ServiceResponse<List<CategoryResponseDto>>.ErrorResponse("No categories found for the given criteria.", 404);
                }

                var categoryDtos = categories.Select(c => new CategoryResponseDto
                {
                    CategoryId = c.category_id,
                    CategoryDescription = c.category_description,
                    CategoryName = c.category_name,
                    CatType = c.cat_type.ToString(),
                    CarrierId = c.carrier_id,
                    CreatedAt = c.created_at
                }).ToList();

                _logger.LogInformation("Successfully retrieved {Count} categories.", categoryDtos.Count);
                return ServiceResponse<List<CategoryResponseDto>>.SuccessResponse(categoryDtos, "Categories retrieved successfully.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid catType value: {CatType}", catType);
                return ServiceResponse<List<CategoryResponseDto>>.ErrorResponse("Invalid catType value.", 400);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while fetching categories.");
                return ServiceResponse<List<CategoryResponseDto>>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<string>> DeleteCategoryAsync(int categoryId, int companyId)
        {
            try
            {
                _logger.LogInformation("Attempting to delete category. CategoryId: {CategoryId}, CompanyId: {CompanyId}", categoryId, companyId);

                int? carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);

                // Check if the category is a custom category and belongs to the carrier
                var category = await _maintenanceCategoryRepository.GetByIdAsync(categoryId);

                if (category == null || category.carrier_id != carrierId)
                {
                    _logger.LogWarning("Category not found or unauthorized. CategoryId: {CategoryId}", categoryId);
                    return ServiceResponse<string>.ErrorResponse("Category not found or you don't have permission to delete it.", 404);
                }

                if (category.cat_type == MaintenanceCategoryType.standard)
                {
                    _logger.LogWarning("Attempt to delete standard category. CategoryId: {CategoryId}", categoryId);
                    return ServiceResponse<string>.ErrorResponse("Deleting a Standard Category is not allowed.", 400);
                }

                // Check if the category is assigned to any issue_ticket
                var issueTickets = await _issueTicketRepository.GetTicketsByCategoryIdAsync(categoryId);

                if (issueTickets?.Any() == true)
                {
                    _logger.LogInformation("CategoryId: {CategoryId} is assigned to {Count} issue tickets. Setting category_id to NULL.", categoryId, issueTickets.Count());

                    // Set category_ID to NULL for those tickets
                    foreach (var ticket in issueTickets)
                    {
                        ticket.category_id = null;
                        await _issueTicketRepository.UpdateAsync(ticket);
                    }
                }

                // Delete the category
                await _maintenanceCategoryRepository.DeleteAsync(category);

                _logger.LogInformation("CategoryId: {CategoryId} deleted successfully.", categoryId);

                return ServiceResponse<string>.SuccessResponse(null, "Category deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while deleting category. CategoryId: {CategoryId}", categoryId);
                return ServiceResponse<string>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }

        public async Task<ServiceResponse<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, int companyId)
        {
            try
            {
                _logger.LogInformation("Creating a new category. CompanyId: {CompanyId}, CategoryName: {CategoryName}", companyId, request.CategoryName);


                // Input validation
                if (string.IsNullOrWhiteSpace(request.CategoryName))
                {
                    _logger.LogWarning("Validation failed: Category name is missing.");
                    return ServiceResponse<CategoryResponseDto>.ErrorResponse("Please enter Name to proceed.", 400);
                }

                if (string.IsNullOrWhiteSpace(request.CategoryDescription))
                {
                    _logger.LogWarning("Validation failed: Category description is missing.");
                    return ServiceResponse<CategoryResponseDto>.ErrorResponse("Please enter Description to proceed.", 400);
                }

                if (request.CategoryDescription.Split(' ').Length > 50)
                {
                    _logger.LogWarning("Category description exceeded 50 words.");
                    return ServiceResponse<CategoryResponseDto>.ErrorResponse("Category description should not exceed 50 words.", 400);
                }

                int? carrierId = await _carrierRepository.GetCarrierIdByCompanyIdAsync(companyId);

                if (carrierId == null)
                {
                    _logger.LogWarning("Carrier not found for CompanyId: {CompanyId}", companyId);
                    return ServiceResponse<CategoryResponseDto>.ErrorResponse("Invalid company ID. Carrier not found.", 400);
                }

                // Create category entity
                var category = new MaintenanceCategory
                {
                    category_name = request.CategoryName,
                    category_description = request.CategoryDescription,
                    carrier_id = carrierId.Value,
                    cat_type = MaintenanceCategoryType.custom,
                    created_at = DateTime.UtcNow
                };

                await _maintenanceCategoryRepository.AddAsync(category);

                _logger.LogInformation("Category created. ID: {CategoryId}", category.category_id);

                // Map entity to response DTO
                var responseDto = new CategoryResponseDto
                {
                    CategoryId = category.category_id,
                    CategoryName = category.category_name,
                    CategoryDescription = category.category_description,
                    CarrierId = category.carrier_id,
                    CatType = category.cat_type.ToString(),
                    CreatedAt = category.created_at
                };

                _logger.LogInformation("Category created successfully. CategoryId: {CategoryId}, CategoryName: {CategoryName}", category.category_id, category.category_name);

                return ServiceResponse<CategoryResponseDto>.SuccessResponse(responseDto, "Category created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating category.");
                return ServiceResponse<CategoryResponseDto>.ErrorResponse("Server is temporarily unavailable. Please try again later.", 503);
            }
        }
    }
}