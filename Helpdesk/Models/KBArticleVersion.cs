using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class KBArticleVersion : IEntity
    {
        public int Id { get; set; }

        public int KBArticleId { get; set; }
        public KBArticle KBArticle { get; set; } = null!;

        public int VersionNumber { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
