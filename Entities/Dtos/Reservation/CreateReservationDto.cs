namespace Entities.Dtos.Reservation
{
    public class CreateReservationDto
    {
        public int VenueId { get; set; }
        public int HallId { get; set; }
        public int MenuId { get; set; }
        public DateOnly EventDate { get; set; }
        public int GuestCount { get; set; }
        public string ContactPhone { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}
