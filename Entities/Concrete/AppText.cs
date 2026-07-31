using Core.Entities;

namespace Entities.Concrete
{
    // İnterfeysdə görünən sabit mətnlər. Əvvəl bunlar kodda böyük lüğətlər kimi
    // saxlanılırdı — bir sözü dəyişmək üçün yenidən yığmaq lazım gəlirdi.
    // İndi baza saxlayır: admin paneldən redaktə olunur, kod isə açar işlədir.
    public class AppText : IEntity
    {
        public int Id { get; set; }

        // audit_action | audit_endpoint | report_reason | page_type | hide_reason
        public string Category { get; set; } = string.Empty;

        // Kodda işlənən açar: "UserLoggedIn", "Auth.Login", "spam"...
        public string Key { get; set; } = string.Empty;

        // Göstərilən mətn
        public string Value { get; set; } = string.Empty;

        // Bəzi kateqoriyalarda ikinci dəyər lazımdır (məsələn endpoint → əməliyyat kodu)
        public string? Extra { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
