namespace Business.Utilities.Email
{
    // appsettings.json-dakı EmailOptions bölməsi.
    // Host boş qalarsa e-poçt göndərilmir, məktub log-a yazılır (development rejimi).
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string From { get; set; } = "no-reply@sadliqsaraylari.az";
        public string FromName { get; set; } = "Şadlıq Sarayları";
        public bool EnableSsl { get; set; } = true;

        // Parol sıfırlama linki bu ünvanla qurulur
        public string FrontendUrl { get; set; } = "http://localhost:3000";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
    }
}
