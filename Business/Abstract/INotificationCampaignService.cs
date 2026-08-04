using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Notification;

namespace Business.Abstract
{
    // Elan göndərişi: admin panelin «Bildiriş göndər» bölməsi.
    public interface INotificationCampaignService
    {
        // Göndərmədən əvvəl: neçə nəfərə gedəcək və kimlərə
        Task<IDataResult<AudiencePreviewDto>> PreviewAsync(AudienceDto audience, int requestedByUserId);

        // Dərhal göndərir və ya tarixə planlaşdırır
        Task<IDataResult<CampaignDto>> SendAsync(CreateCampaignDto dto, int senderUserId);

        // Yalnız göndərənə: özünə sınaq göndərişi
        Task<IResult> SendTestAsync(CreateCampaignDto dto, int senderUserId);

        Task<IDataResult<PagedResult<CampaignDto>>> SearchAsync(CampaignQueryDto query);
        Task<IDataResult<CampaignDto>> GetAsync(int campaignId);

        // Planlaşdırılmış göndərişi ləğv edir
        Task<IResult> CancelAsync(int campaignId);

        // Arxa fon xidməti üçün: vaxtı çatmış göndərişləri icra edir
        Task<int> DispatchDueAsync(CancellationToken token = default);
    }
}
