using Core.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Entities.Concrete
{
    public class User : IEntity
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Bazada saxlanılmır, yalnız göstərmək üçün hesablanır (token, rəy müəllifi, interfeys)
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // API cavablarında heç vaxt görünməməlidir
        [JsonIgnore]
        public byte[] PasswordHash { get; set; }
        [JsonIgnore]
        public byte[] PasswordSalt { get; set; }

        public UserRole Role { get; set; } = UserRole.Customer;

        // Bloklanmış istifadəçi sistemə girə bilmir, amma məlumatları silinmir
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
