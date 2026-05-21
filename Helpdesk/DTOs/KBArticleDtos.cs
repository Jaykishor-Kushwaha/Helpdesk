using Helpdesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace Helpdesk.DTOs
{
    public class KBArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public int HelpfulCount { get; set; }
        public int NotHelpfulCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class CreateKBArticleDto
    {
        [Required] [MaxLength(200)] public string Title { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        [Required] public string Content { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public KBArticleStatus Status { get; set; } = KBArticleStatus.Draft;
    }

    public class UpdateKBArticleDto : GetByIdDto
    {
        [MaxLength(200)] public string? Title { get; set; }
        public string? Tags { get; set; }
        public string? Content { get; set; }
        public int? CategoryId { get; set; }
        public KBArticleStatus? Status { get; set; }
    }

    public class KBArticleVersionDto
    {
        public int Id { get; set; }
        public int KBArticleId { get; set; }
        public int VersionNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class KBFeedbackDto
    {
        [Required]
        public bool IsHelpful { get; set; }
    }

    public class KBRejectDto
    {
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
