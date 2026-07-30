namespace Entities.Dtos.Audit
{
    // Jurnal səhifəsinin süzgəcləri
    public class AuditSearchDto
    {
        public string? Q { get; set; }           // izah və istifadəçi adı üzrə axtarış
        public string? Action { get; set; }      // əməliyyat kodu
        public string? Role { get; set; }        // əməliyyatı edənin rolu
        public int? UserId { get; set; }
        public int? VenueId { get; set; }
        public string? EntityType { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Yalnız uğursuz əməliyyatlar
        public bool? OnlyFailed { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
    }

    // Süzgəc siyahıları üçün
    public class AuditFilterOptionsDto
    {
        public List<AuditActionOption> Actions { get; set; } = new();
    }

    public class AuditActionOption
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
