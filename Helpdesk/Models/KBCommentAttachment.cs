using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class KBCommentAttachment : IEntity
    {
        public int Id { get; set; }
        
        public int CommentId { get; set; }
        public Comment? Comment { get; set; }

        public int KBArticleId { get; set; }
        public KBArticle? KBArticle { get; set; }

        public int AttachedByUserId { get; set; }
        public User? AttachedByUser { get; set; }

        public DateTime AttachedAt { get; set; } = DateTime.UtcNow;
    }
}
