using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface IRecurringTemplateService
    {
        Task<PagedResponse<RecurringTemplateDto>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<RecurringTemplateDto?> GetByIdAsync(int id);
        Task<RecurringTemplateDto> CreateAsync(CreateRecurringTemplateDto dto);
        Task<RecurringTemplateDto?> UpdateAsync(int id, UpdateRecurringTemplateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> TriggerNowAsync(int id);
    }
}
