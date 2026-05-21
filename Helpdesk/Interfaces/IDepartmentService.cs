using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllAsync();
        Task<DepartmentDto?> GetByIdAsync(int id);
        Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
        Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentDto dto);
        Task<bool> DeactivateAsync(int id);
        Task<IEnumerable<DepartmentSummaryDto>> GetDepartmentSummaryAsync();
    }
}
