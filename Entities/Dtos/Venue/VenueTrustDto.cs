namespace Entities.Dtos.Venue
{
    // Müştəriyə göstərilən etibar siqnalları. Bu rəqəmlər əvvəl yalnız sarayın
    // öz hesabatında var idi — müştəri isə sorğu göndərməzdən əvvəl sarayın
    // cavab verib-verməyəcəyini bilmirdi.
    //
    // Az sayda sorğusu olan saray üçün faiz yanıltıcıdır (1 sorğu = 0% və ya
    // 100%), ona görə nümunə azdırsa sahə boş qaytarılır.
    public class VenueTrustDto
    {
        public int VenueId { get; set; }

        // Sorğuya orta cavab müddəti (saat). Cavablanmış sorğu yoxdursa null.
        public double? AverageResponseHours { get; set; }

        // Sorğuların neçə faizini təsdiqləyir. Nümunə azdırsa null.
        public int? ConfirmPercent { get; set; }

        // Cavabsız qoyub bağlanan sorğuların sayı — «yazsam cavab verəcəklərmi»
        public int ExpiredCount { get; set; }

        // Təsdiqdən sonra saray tərəfindən ləğv edilən tədbirlər.
        // Ləğv səbəbi məlumatına məna verən yer budur.
        public int CancelledAfterConfirmCount { get; set; }

        // Rəqəmlərin hansı sayda sorğuya əsaslandığı — şəffaflıq üçün
        public int BasedOnRequests { get; set; }

        // Hesablama üçün kifayət qədər məlumat varmı
        public bool HasEnoughData { get; set; }
    }
}
