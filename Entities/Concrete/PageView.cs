using Core.Entities;

namespace Entities.Concrete
{
    // Saytdakı bir səhifə baxışı. Fəaliyyət jurnalından tamamilə ayrıdır:
    // jurnal «kim nə dəyişdi» sualına, bu cədvəl isə «kim nəyə baxdı»
    // sualına cavab verir. Qeydiyyatsız ziyarətçilər də bura düşür.
    public class PageView : IEntity
    {
        public int Id { get; set; }

        // Brauzerdə saxlanılan təsadüfi ID — eyni ziyarətçinin addımlarını
        // bir-birinə bağlamağa imkan verir, şəxsiyyəti açmır
        public string SessionId { get; set; } = string.Empty;

        // Ziyarətçi giriş edibsə doludur, qonaqda boşdur
        public int? UserId { get; set; }
        public User? User { get; set; }

        // "home", "venue", "search", "profile" və s.
        public string PageType { get; set; } = string.Empty;

        // Frontdakı ünvan, məsələn /venues/12
        public string Path { get; set; } = string.Empty;

        // Saray səhifəsidirsə hansı saray
        public int? VenueId { get; set; }

        // Səhifənin adı — hesabatda oxunaqlı olsun deyə
        public string? Title { get; set; }

        // Ziyarətçi hansı saytdan gəlib
        public string? Referrer { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
