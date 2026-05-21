namespace Helpdesk.Models
{
    public class Category : IEntity
    {

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    }
}
