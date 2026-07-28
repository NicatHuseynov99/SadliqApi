using Core.Entities;

namespace Entities.Concrete
{
    // saray haqqında qonaq rəyi
    public class Review : IEntity
    {
        public int Id { get; set; }
        public int VenueId { get; set; }
        public Venue? Venue { get; set; }

        // Rəyi yazan qeydiyyatlı istifadəçidirsə saxlanılır (köçürülmüş rəylərdə boş ola bilər)
        public int? CustomerId { get; set; }
        public User? Customer { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
