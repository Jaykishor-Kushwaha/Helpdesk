using System.ComponentModel.DataAnnotations;

namespace Helpdesk.Models
{
    public class Department : IEntity
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? DepartmentHeadId { get; set; }
        public User? DepartmentHead { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsGeneral => Name.Equals("General", StringComparison.OrdinalIgnoreCase);
        public bool CanDeactivate => !IsGeneral;
        
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
