namespace Business.Abstract
{
    // İnterfeys mətnlərinin kataloqu. Mətnlər bazadadır, amma hər sorğuda baza
    // oxumaq mənasızdır — burada yaddaşda saxlanılır və dəyişəndə təzələnir.
    public interface ITextCatalog
    {
        // Bir açarın mətni; tapılmasa açarın özü qaytarılır
        string Get(string category, string key);

        // Açar → mətn (süzgəc siyahıları üçün)
        IReadOnlyDictionary<string, string> Map(string category);

        // Açar → (mətn, əlavə dəyər) — endpoint lüğəti kimi iki dəyərli kateqoriyalar üçün
        bool TryGetPair(string category, string key, out string value, out string? extra);

        // Baza dəyişəndə çağırılır
        void Invalidate();
    }

    // Kataloqun kateqoriya adları bir yerdə saxlanılır ki, yazılış səhvi olmasın
    public static class TextCategories
    {
        public const string AuditAction = "audit_action";
        public const string AuditEndpoint = "audit_endpoint";
        public const string ReportReason = "report_reason";
        public const string PageType = "page_type";
        public const string HideReason = "hide_reason";
        public const string TableLabel = "table_label";
    }
}
