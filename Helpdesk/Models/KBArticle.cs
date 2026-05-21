using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class KBArticle : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public Helpdesk.Enums.KBArticleStatus Status { get; set; } = Helpdesk.Enums.KBArticleStatus.Draft;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;

        public int ViewCount { get; set; } = 0;
        public int HelpfulCount { get; set; } = 0;
        public int NotHelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<KBArticleVersion> Versions { get; set; } = new List<KBArticleVersion>();
    }
}
