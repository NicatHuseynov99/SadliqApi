namespace Entities.Dtos.Reservation
{
    // Sahibin göndərdiyi əks-təklif. Boş qoyulan sahə «dəyişmir» deməkdir.
    public class CreateCounterOfferDto
    {
        public int ReservationId { get; set; }

        public DateOnly? EventDate { get; set; }
        public int? HallId { get; set; }
        public int? MenuId { get; set; }
        public decimal? PricePerPerson { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
