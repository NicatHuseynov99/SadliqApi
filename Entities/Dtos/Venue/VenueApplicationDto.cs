namespace Entities.Dtos.Venue
{
    // Sahibkarın müraciəti: bütün addımların məlumatı bir sorğuda gəlir.
    // Səbəb: yarımçıq saray yaranmasın — ya hamısı yazılır, ya heç nə.
    public class VenueApplicationDto
    {
        // Hesab. İstifadəçi artıq daxil olubsa boş qalır və mövcud hesab işlədilir.
        public ApplicantDto? Applicant { get; set; }

        public ApplicationVenueDto Venue { get; set; } = new();

        // Ən azı bir zal tələb olunur — zalsız saray rezerv qəbul edə bilmir
        public List<ApplicationHallDto> Halls { get; set; } = new();

        // Menyu məcburi deyil, sonra əlavə oluna bilər
        public List<ApplicationMenuDto> Menus { get; set; } = new();
    }

    public class ApplicantDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ApplicationVenueDto
    {
        // Məcburi
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Məcburi deyil — sonra da doldurula bilər
        public string Description { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? ParkingCapacity { get; set; }
        public bool HasValetParking { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public List<string> Features { get; set; } = new();
    }

    public class ApplicationHallDto
    {
        public string Name { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public decimal MinPrice { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new();
    }

    public class ApplicationMenuDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal PricePerPerson { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    // Müraciət göndəriləndən sonra sahibkara qaytarılan cavab
    public class VenueApplicationResultDto
    {
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Yeni hesab yaradılıbsa sahibkar dərhal daxil olsun deyə token qaytarılır
        public string? Token { get; set; }

        // Təsdiqdən əvvəl tamamlanması tövsiyə olunan sahələr
        public List<string> Missing { get; set; } = new();
    }
}
