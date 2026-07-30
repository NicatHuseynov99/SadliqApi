namespace Entities.Concrete
{
    // 0 = adi müştəri
    // 1 = saray rəhbərliyi — yalnız öz sarayları
    // 2 = platformanın sahibi — hər şey
    // 3 = saray administratoru — rəhbərliyin təyin etdiyi, bir saray üzrə məhdud səlahiyyət
    // 4 = dəstək admini — platformanın işçisi, bütün saraylarda rəhbərlik işi görür,
    //     amma istifadəçi rollarına və platforma parametrlərinə toxuna bilmir
    public enum UserRole { Customer, Owner, SuperAdmin, VenueAdmin, Support }
    public enum MediaType { Image, Video }
    public enum AvailabilityStatus { Available, Booked, Held }
    public enum ReservationStatus { Pending, Confirmed, Rejected, Cancelled }
}
