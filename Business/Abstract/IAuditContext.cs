namespace Business.Abstract
{
    // Servis kodu jurnala insan dilində izah və obyekt məlumatı əlavə etmək üçün bundan istifadə edir.
    // Heç nə yazılmasa, filtr endpoint adına görə avtomatik mətn qoyur.
    public interface IAuditContext
    {
        string? Action { get; }
        string? Description { get; }
        string? EntityType { get; }
        int? EntityId { get; }
        int? VenueId { get; }

        void Set(string action, string description, string? entityType = null, int? entityId = null, int? venueId = null);

        // Giriş və qeydiyyat kimi hallarda istifadəçi hələ token daşımır —
        // əməliyyatı edəni servis özü bildirir
        int? ActorId { get; }
        string? ActorName { get; }
        int? ActorRole { get; }

        void SetActor(int userId, string fullName, int role);
    }
}
