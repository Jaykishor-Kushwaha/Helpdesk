using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface ISurveyService
    {
        Task<IEnumerable<SurveyResponseDto>> GetAllAsync(int? ticketId = null);
        Task<SurveyResponseDto> CreateAsync(CreateSurveyResponseDto dto);
    }
}
