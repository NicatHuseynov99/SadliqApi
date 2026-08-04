using Entities.Concrete;

namespace Entities.Dtos.Notification
{
    // Zəng siyahısındakı bir sətir
    public class NotificationDto
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Siyahı + oxunmamış sayı bir sorğuda gəlsin
    public class NotificationFeedDto
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMore => Page * PageSize < TotalCount;
    }

    public class NotificationQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Yalnız oxunmamışlar
        public bool UnreadOnly { get; set; }
    }

    // İstifadəçinin öz seçimləri
    public class NotificationPreferenceDto
    {
        public bool AllowAnnouncements { get; set; } = true;
        public bool AllowEmail { get; set; } = true;
        public bool AllowSms { get; set; } = true;
    }

    // Auditoriya şərtləri. Hamısı boşdursa — bütün aktiv istifadəçilər.
    public class AudienceDto
    {
        // Boş = fərq etmir
        public List<UserRole> Roles { get; set; } = new();
        public List<string> Cities { get; set; } = new();

        public AudienceBehavior Behavior { get; set; } = AudienceBehavior.Any;

        // Davranış süzgəcinin pəncərəsi (gün)
        public int BehaviorDays { get; set; } = 30;

        // Yalnız bu sarayın müştəriləri (saray sahibi öz qonaqlarına yazanda)
        public int? VenueId { get; set; }

        // Sınaq göndərişi: yalnız bu istifadəçilərə
        public List<int> UserIds { get; set; } = new();
    }

    public class CreateCampaignDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Link { get; set; }

        public AudienceDto Audience { get; set; } = new();

        public NotificationChannels Channels { get; set; } = NotificationChannels.InApp;

        // Boşdursa dərhal göndərilir
        public DateTime? ScheduledAt { get; set; }
    }

    // Göndərmədən əvvəl «neçə nəfərə gedəcək» cavabı
    public class AudiencePreviewDto
    {
        public int Count { get; set; }
        public string Summary { get; set; } = string.Empty;

        // İlk bir neçə nəfər — admin kimə getdiyini gözü ilə görsün
        public List<string> Sample { get; set; } = new();
    }

    public class CampaignDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string AudienceSummary { get; set; } = string.Empty;
        public string ChannelsLabel { get; set; } = string.Empty;
        public CampaignStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime? ScheduledAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; } = string.Empty;

        public int RecipientCount { get; set; }
        public int ReadCount { get; set; }
        public int ClickCount { get; set; }

        // Faizlər — panel özü hesablamasın
        public int ReadPercent => RecipientCount == 0 ? 0 : ReadCount * 100 / RecipientCount;
        public int ClickPercent => RecipientCount == 0 ? 0 : ClickCount * 100 / RecipientCount;

        public string? Error { get; set; }
    }

    public class CampaignQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Status { get; set; }
        public string? Search { get; set; }
    }
}
