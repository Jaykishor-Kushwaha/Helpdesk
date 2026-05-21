using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface ICommentService
    {
        Task<IEnumerable<CommentResponseDto>> GetCommentsByTicketIdAsync(GetCommentsByTicketDto dto);

        Task<CommentResponseDto> AddCommentAsync(AddCommentDto dto);

        Task<bool> DeleteCommentAsync(CommentActionDto dto);
    }
}