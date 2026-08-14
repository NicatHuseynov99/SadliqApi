namespace Entities.Dtos.Reservation
{
    // Birbaşa rezerv yazışmasına mesaj. Rezervin Id-si marşrutdan gəlir,
    // ona görə burada yalnız mətn var.
    public class SendReservationMessageDto
    {
        public string Body { get; set; } = string.Empty;
    }
}
