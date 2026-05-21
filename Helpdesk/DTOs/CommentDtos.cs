using System.ComponentModel.DataAnnotations;
using Helpdesk.Enums;

namespace Helpdesk.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int TicketId { get; set; }
    }

    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TicketId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }

    // ✅ Only ID needed (user comes from JWT)
    public class CommentActionDto : GetByIdDto
    {
    }

    public class GetCommentsByTicketDto
    {
        public int TicketId { get; set; }
    }

    public class AddCommentDto
    {
        public int TicketId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}