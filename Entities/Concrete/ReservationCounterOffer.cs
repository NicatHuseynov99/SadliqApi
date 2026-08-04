using Core.Entities;

namespace Entities.Concrete
{
    // Sahibin «rədd» əvəzinə göndərdiyi əks-təklif: başqa tarix, başqa zal,
    // başqa menyu və ya başqa qiymət. Müştəri qəbul edir və ya rədd edir.
    //
    // Real həyatda sahibin cavabı çox vaxt «yox» deyil, «o gün dolu, 19-u boşdur»
    // olur — bu cədvəl həmin söhbəti platformada saxlayır.
    public class ReservationCounterOffer : IEntity, ISoftDelete
    {
        public int Id { get; set; }

        public int ReservationId { get; set; }
        public Reservation? Reservation { get; set; }

        // Təklifi göndərən (sahib və ya saray administratoru)
        public int CreatedByUserId { get; set; }

        // Boş olan sahə «dəyişmir» deməkdir
        public DateOnly? EventDate { get; set; }
        public int? HallId { get; set; }
        public Hall? Hall { get; set; }
        public int? MenuId { get; set; }
        public Menu? Menu { get; set; }

        // Menyu qiymətindən fərqli razılaşdırılmış qiymət
        public decimal? PricePerPerson { get; set; }

        public string Message { get; set; } = string.Empty;

        public CounterOfferStatus Status { get; set; } = CounterOfferStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
