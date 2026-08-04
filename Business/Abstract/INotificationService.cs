using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Notification;

namespace Business.Abstract
{
    // Bir bildirişin məzmunu. Kanallardan asılı deyil — eyni mətn
    // həm saytda, həm e-poçtda, sabah SMS-də işlənir.
    public record NotificationMessage(
        NotificationType Type,
        string Title,
        string Body,
        string? Link = null,
        int? CampaignId = null);

    // Bildirişi kanala çatdıran vahid. Yeni kanal əlavə etmək üçün
    // bu interfeysin yeni icrasını yazıb qeydiyyatdan keçirmək kifayətdir.
    public interface INotificationChannel
    {
        NotificationChannels Kind { get; }

        Task SendAsync(User user, NotificationMessage message, CancellationToken token = default);
    }

    // Sistemin qalan hissəsi yalnız bunu tanıyır: «bu adama bunu bildir».
    // Hansı kanalların açıq olduğunu, istifadəçinin seçimlərini burası bilir.
    public interface INotificationService
    {
        // Hazırda yalnız saytdaxili bildiriş işlədilir. E-poçt və SMS kanalları
        // hazırdır — lazım olanda çağırışda göstərmək kifayətdir.
        Task NotifyAsync(int userId, NotificationMessage message, NotificationChannels channels = NotificationChannels.InApp);

        Task NotifyManyAsync(IEnumerable<int> userIds, NotificationMessage message, NotificationChannels channels);

        // İstifadəçinin öz bildirişləri
        Task<IDataResult<NotificationFeedDto>> GetFeedAsync(int userId, NotificationQueryDto query);
        Task<IDataResult<int>> GetUnreadCountAsync(int userId);
        Task<IResult> MarkReadAsync(int notificationId, int userId);
        Task<IResult> MarkAllReadAsync(int userId);
        Task<IResult> RegisterClickAsync(int notificationId, int userId);
        Task<IResult> DeleteAsync(int notificationId, int userId);

        // Seçimlər
        Task<IDataResult<NotificationPreferenceDto>> GetPreferencesAsync(int userId);
        Task<IResult> UpdatePreferencesAsync(int userId, NotificationPreferenceDto dto);
    }
}
