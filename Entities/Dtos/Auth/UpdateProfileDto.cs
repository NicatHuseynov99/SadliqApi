namespace Entities.Dtos.Auth
{
    // İstifadəçinin öz profil məlumatlarını yeniləməsi (email dəyişdirilmir)
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
