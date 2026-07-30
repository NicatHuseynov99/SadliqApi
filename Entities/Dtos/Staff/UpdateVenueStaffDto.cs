namespace Entities.Dtos.Staff
{
    // Administratorun ad, soyad və telefonunu yeniləmək üçün (e-poçt hesabın açarıdır, dəyişmir)
    public class UpdateVenueStaffDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
