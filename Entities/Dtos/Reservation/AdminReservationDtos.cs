using Entities.Concrete;

namespace Entities.Dtos.Reservation
{
    // Platforma üzrə rezerv axtarışının süzgəcləri
    public class AdminReservationSearchDto
    {
        // Müştəri adı, telefon, saray adı və ya qeyd üzrə axtarış
        public string? Q { get; set; }

        // Pending | Confirmed | Rejected | Cancelled
        public string? Status { get; set; }
        public int? VenueId { get; set; }

        // Tədbir tarixi aralığı
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        // upcoming — yalnız gələcək tədbirlər, past — keçmişlər
        public string? Period { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class AdminReservationDto
    {
        public int Id { get; set; }

        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Paneldə sarayı bir baxışda tanımaq üçün
        public string? VenueImageUrl { get; set; }
        public string City { get; set; } = string.Empty;
        public string HallName { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string ContactPhone { get; set; } = string.Empty;

        public DateOnly EventDate { get; set; }
        public int GuestCount { get; set; }
        public string? Note { get; set; }

        public ReservationStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Tədbir keçibsə statusu dəyişməyin mənası yoxdur — panel bunu göstərir
        public bool IsPast { get; set; }

        // Silinmiş rezerv yalnız adminə görünür və geri qaytarıla bilir
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    public class AdminReservationStatsDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Confirmed { get; set; }
        public int Rejected { get; set; }
        public int Cancelled { get; set; }

        // Bu gündən sonrakı təsdiqlənmiş tədbirlər
        public int Upcoming { get; set; }

        // 3 gündən çoxdur cavabsız qalan sorğular — sahiblərə xatırlatmaq üçün
        public int StalePending { get; set; }

        public List<VenueReservationCountDto> TopVenues { get; set; } = new();
    }

    public class VenueReservationCountDto
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Pending { get; set; }
    }
}
