using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryResponseDto>> GetAllCategoriesAsync();

        Task<CategoryResponseDto> GetCategoryByIdAsync(GetByIdDto dto);

        Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto);

        Task UpdateCategoryAsync(UpdateCategoryDto dto);

        Task DeleteCategoryAsync(GetByIdDto dto);
    }
}