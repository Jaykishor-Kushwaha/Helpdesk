using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface ITicketService
    {
        Task<PagedResponse<TicketResponseDto>> FilterTicketsAsync(TicketFilterDto filter);

        Task<TicketResponseDto?> GetTicketByIdAsync(GetTicketByIdDto dto);

        // ✅ FIXED
        Task<int> CreateTicketAsync(CreateTicketDto dto);

        Task<TicketResponseDto?> UpdateTicketAsync(UpdateTicketDto dto);

        Task<bool> DeleteTicketAsync(GetByIdDto dto);

        Task<bool> EscalateTicketAsync(EscalateTicketDto dto);

        Task<bool> AcknowledgeEscalationAsync(int ticketId);

        Task<bool> ReopenTicketAsync(int ticketId);

        Task<bool> OverrideSlaAsync(int ticketId, OverrideSlaDto dto);

        Task<bool> ResolveViaKBAsync(int ticketId, int kbArticleId);
    }
}