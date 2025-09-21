using OmmoBackend.Dtos;
using OmmoBackend.Helpers.Responses;

namespace OmmoBackend.Services.Interfaces
{
    public interface IMaintenanceCategoryService
    {
        Task<ServiceResponse<List<CategoryResponseDto>>> GetCategoriesAsync(string? catType, int? carrierId, int companyId);
        Task<ServiceResponse<string>> DeleteCategoryAsync(int categoryId, int companyId);
        Task<ServiceResponse<CategoryResponseDto>> CreateCategoryAsync(CreateCategoryRequest request, int carrierId);
    }
}