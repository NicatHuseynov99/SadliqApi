namespace Entities.Concrete
{
    // 0 = adi müştəri, 1 = saray rəhbərliyi, 2 = platformanın sahibi,
    // 3 = rəhbərliyin təyin etdiyi saray administratoru
    public enum UserRole { Customer, Owner, SuperAdmin, VenueAdmin }
    public enum MediaType { Image, Video }
    public enum AvailabilityStatus { Available, Booked, Held }
    public enum ReservationStatus { Pending, Confirmed, Rejected, Cancelled }
}
