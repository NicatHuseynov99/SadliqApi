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
    // Expired = sahib vaxtında cavab vermədi, sorğu öz-özünə bağlandı və tarix boşaldı
    // CounterOffered = sahib əks-təklif göndərib, müştərinin cavabı gözlənilir
    public enum ReservationStatus { Pending, Confirmed, Rejected, Cancelled, Expired, CounterOffered }

    public enum CounterOfferStatus { Pending, Accepted, Declined, Withdrawn }

    // Tərs axtarış: müştərinin sorğusu və sarayların təklifi
    public enum VenueRequestStatus { Open, Fulfilled, Closed, Expired }

    public enum VenueOfferStatus { Sent, Accepted, Declined, Withdrawn }

    // Saray administratorunun səlahiyyətləri. Sahib hamısına malikdir;
    // administratora rəhbər ayrıca verir.
    public enum StaffPermission
    {
        // Rezervi təsdiqləmək və rədd etmək
        ConfirmReservations,

        // Zalın təqvimində günü bağlamaq/açmaq
        EditCalendar,

        // Əks-təklif və açıq sorğulara təklif göndərmək
        SendOffers
    }

    // Şikayət/mübahisə. Müştəri saraydan, saray müştəridən şikayət edə bilər —
    // qərarı platforma verir və qərarın izi qalır.
    //
    // InReview = dəstək işi öz üzərinə götürüb
    // Resolved = qərar verilib (nəticə mətndə)
    // Closed = əsassız və ya şikayətçi geri götürüb
    public enum DisputeStatus { Open, InReview, Resolved, Closed }

    // Şikayətin mövzusu — dəstək növbəsini süzgəcləmək üçün
    public enum DisputeCategory
    {
        // Rezervlə bağlı: ləğv, tarix, şərtlərin pozulması
        Reservation = 0,

        // Sarayın davranışı: cavabsızlıq, verilən sözdən dönmə
        VenueBehavior = 1,

        // Müştərinin davranışı: gəlməmə, təhqir
        CustomerBehavior = 2,

        // Qiymət və ödənişlə bağlı fikir ayrılığı
        Payment = 3,

        Other = 9
    }

    // Bildirişin növü. «Announcement»-dan başqa hamısı sistem bildirişidir —
    // xidmətin bir hissəsi olduğu üçün söndürülə bilmir.
    public enum NotificationType
    {
        Announcement = 0,
        ReservationCreated = 1,
        ReservationStatusChanged = 2,
        ReservationCancelled = 3,
        ReservationReminder = 4,
        ReservationExpired = 5,
        VenueApproved = 6,
        VenueRejected = 7,
        ReviewReceived = 8,
        SystemMessage = 9
    }

    // Bir bildiriş eyni anda bir neçə kanaldan gedə bilər
    [Flags]
    public enum NotificationChannels
    {
        None = 0,
        InApp = 1,
        Email = 2,
        Sms = 4
    }

    public enum CampaignStatus { Draft, Scheduled, Sending, Sent, Failed, Cancelled }

    // Auditoriyanı davranışa görə daraltmaq üçün hazır süzgəclər
    public enum AudienceBehavior
    {
        Any = 0,
        ReservedRecently = 1,      // son N gündə rezerv sorğusu göndərənlər
        FavoritedNotReserved = 2,  // saray bəyənib, amma sorğu göndərməyənlər
        ReservationRejected = 3,   // sorğusu rədd edilmiş və ya vaxtı keçmiş
        Inactive = 4,              // N gündür heç bir hərəkəti olmayanlar
        NeverReserved = 5          // heç vaxt sorğu göndərməyənlər
    }
}
