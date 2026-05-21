using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class SystemSetting : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
