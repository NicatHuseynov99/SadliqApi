using Business.Abstract;

namespace Business.Concrete
{
    // Sorğu boyunca yaşayan sadə saxlayıcı (scoped)
    public class AuditContext : IAuditContext
    {
        public string? Action { get; private set; }
        public string? Description { get; private set; }
        public string? EntityType { get; private set; }
        public int? EntityId { get; private set; }
        public int? VenueId { get; private set; }

        public int? ActorId { get; private set; }
        public string? ActorName { get; private set; }
        public int? ActorRole { get; private set; }

        public void SetActor(int userId, string fullName, int role)
        {
            ActorId = userId;
            ActorName = fullName;
            ActorRole = role;
        }

        public void Set(string action, string description, string? entityType = null, int? entityId = null, int? venueId = null)
        {
            Action = action;
            Description = description;
            EntityType = entityType;
            EntityId = entityId;
            VenueId = venueId;
        }
    }
}
