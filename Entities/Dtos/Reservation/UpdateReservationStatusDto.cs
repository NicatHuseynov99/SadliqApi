using Entities.Concrete;

namespace Entities.Dtos.Reservation
{
    public class UpdateReservationStatusDto
    {
        public ReservationStatus Status { get; set; }

        // Təsdiqlənmiş tədbir ləğv olunanda məcburidir. Admin müdaxiləsində
        // isə həmişə tələb olunur — kənardan edilən dəyişiklik izah istəyir.
        public string? Reason { get; set; }
    }

    // Müştərinin öz rezervini ləğv etməsi
    public class CancelReservationDto
    {
        // Təsdiqlənmiş tədbir ləğv olunursa məcburidir
        public string? Reason { get; set; }
    }
}
