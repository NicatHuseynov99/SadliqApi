using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Concrete.Notifications
{
    // Bildiriş axınının mərkəzi. İki işi var:
    //   1) məzmunu istifadəçinin icazə verdiyi kanallara paylayır;
    //   2) istifadəçiyə öz bildirişlərini göstərir.
    public class NotificationManager : INotificationService
    {
        private readonly INotificationDal _dal;
        private readonly IUserDal _userDal;
        private readonly INotificationPreferenceDal _preferenceDal;
        private readonly IEnumerable<INotificationChannel> _channels;
        private readonly ILogger<NotificationManager> _logger;

        public NotificationManager(
            INotificationDal dal,
            IUserDal userDal,
            INotificationPreferenceDal preferenceDal,
            IEnumerable<INotificationChannel> channels,
            ILogger<NotificationManager> logger)
        {
            _dal = dal;
            _userDal = userDal;
            _preferenceDal = preferenceDal;
            _channels = channels;
            _logger = logger;
        }

        // ————— göndərmə —————

        public async Task NotifyAsync(
            int userId,
            NotificationMessage message,
            NotificationChannels channels = NotificationChannels.InApp)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null || !user.IsActive) return;

            await DeliverAsync(user, message, channels);
        }

        public async Task NotifyManyAsync(
            IEnumerable<int> userIds, NotificationMessage message, NotificationChannels channels)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0) return;

            var query = await _userDal.GetListAsync(u => ids.Contains(u.Id) && u.IsActive);
            var users = await query.ToListAsync();

            var preferences = await LoadPreferencesAsync(ids);

            // Saytdaxili bildirişlər bir sorğu ilə yazılır — minlərlə sətir üçün vacibdir
            if (channels.HasFlag(NotificationChannels.InApp))
            {
                var rows = users
                    .Where(u => IsAllowed(message.Type, preferences.GetValueOrDefault(u.Id)))
                    .Select(u => new Notification
                    {
                        UserId = u.Id,
                        Type = message.Type,
                        Title = message.Title,
                        Body = message.Body,
                        Link = message.Link,
                        CampaignId = message.CampaignId,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (rows.Count > 0)
                    await _dal.AddManyAsync(rows);
            }

            // Qalan kanallar bir-bir gedir — biri xəta versə digərləri dayanmır
            var rest = channels & ~NotificationChannels.InApp;
            if (rest == NotificationChannels.None) return;

            foreach (var user in users)
                await DeliverAsync(user, message, rest, preferences.GetValueOrDefault(user.Id));
        }

        private async Task DeliverAsync(
            User user,
            NotificationMessage message,
            NotificationChannels requested,
            NotificationPreference? preference = null)
        {
            preference ??= await GetOrCreatePreferenceAsync(user.Id);

            if (!IsAllowed(message.Type, preference))
                return;

            foreach (var channel in _channels)
            {
                if (!requested.HasFlag(channel.Kind)) continue;
                if (!IsChannelAllowed(channel.Kind, preference)) continue;

                try
                {
                    await channel.SendAsync(user, message);
                }
                catch (Exception ex)
                {
                    // Bir kanalın xətası bildirişin qalan hissəsini dayandırmamalıdır
                    _logger.LogError(ex, "Bildiriş göndərilmədi: {Channel} → {UserId}", channel.Kind, user.Id);
                }
            }
        }

        // Elanlar söndürülə bilər, sistem bildirişləri yox
        private static bool IsAllowed(NotificationType type, NotificationPreference? preference) =>
            type != NotificationType.Announcement || preference?.AllowAnnouncements != false;

        private static bool IsChannelAllowed(NotificationChannels kind, NotificationPreference? preference) => kind switch
        {
            NotificationChannels.Email => preference?.AllowEmail != false,
            NotificationChannels.Sms => preference?.AllowSms != false,
            _ => true
        };

        // ————— oxuma —————

        public async Task<IDataResult<NotificationFeedDto>> GetFeedAsync(int userId, NotificationQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 50);

            var all = await _dal.GetListAsync(n => n.UserId == userId);

            if (query.UnreadOnly)
                all = all.Where(n => !n.IsRead);

            var total = await all.CountAsync();

            var items = await all
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    TypeLabel = TypeLabel(n.Type),
                    Title = n.Title,
                    Body = n.Body,
                    Link = n.Link,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync();

            return new SuccessDataResult<NotificationFeedDto>(new NotificationFeedDto
            {
                Items = items,
                TotalCount = total,
                UnreadCount = await _dal.GetUnreadCountAsync(userId),
                Page = page,
                PageSize = size
            });
        }

        public async Task<IDataResult<int>> GetUnreadCountAsync(int userId) =>
            new SuccessDataResult<int>(await _dal.GetUnreadCountAsync(userId));

        public async Task<IResult> MarkReadAsync(int notificationId, int userId)
        {
            var notification = await _dal.GetAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification is null) return new ErrorResult("Bildiriş tapılmadı.");

            if (notification.IsRead) return new SuccessResult();

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dal.UpdateAsync(notification);

            if (notification.CampaignId is int campaignId)
                await _dal.RefreshCampaignStatsAsync(campaignId);

            return new SuccessResult("Oxundu.");
        }

        public async Task<IResult> MarkAllReadAsync(int userId)
        {
            var count = await _dal.MarkAllReadAsync(userId);
            return new SuccessResult($"{count} bildiriş oxunmuş sayıldı.");
        }

        // İstifadəçi bildirişdəki linkə keçdi — elanın nəticəsi ölçülür
        public async Task<IResult> RegisterClickAsync(int notificationId, int userId)
        {
            var notification = await _dal.GetAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification is null) return new ErrorResult("Bildiriş tapılmadı.");

            notification.ClickedAt ??= DateTime.UtcNow;
            notification.IsRead = true;
            notification.ReadAt ??= DateTime.UtcNow;
            await _dal.UpdateAsync(notification);

            if (notification.CampaignId is int campaignId)
                await _dal.RefreshCampaignStatsAsync(campaignId);

            return new SuccessResult();
        }

        public async Task<IResult> DeleteAsync(int notificationId, int userId)
        {
            var notification = await _dal.GetAsync(n => n.Id == notificationId && n.UserId == userId);
            if (notification is null) return new ErrorResult("Bildiriş tapılmadı.");

            await _dal.DeleteAsync(notification);
            return new SuccessResult("Bildiriş silindi.");
        }

        // ————— seçimlər —————

        public async Task<IDataResult<NotificationPreferenceDto>> GetPreferencesAsync(int userId)
        {
            var preference = await GetOrCreatePreferenceAsync(userId);

            return new SuccessDataResult<NotificationPreferenceDto>(new NotificationPreferenceDto
            {
                AllowAnnouncements = preference.AllowAnnouncements,
                AllowEmail = preference.AllowEmail,
                AllowSms = preference.AllowSms
            });
        }

        public async Task<IResult> UpdatePreferencesAsync(int userId, NotificationPreferenceDto dto)
        {
            var preference = await GetOrCreatePreferenceAsync(userId);

            preference.AllowAnnouncements = dto.AllowAnnouncements;
            preference.AllowEmail = dto.AllowEmail;
            preference.AllowSms = dto.AllowSms;
            preference.UpdatedAt = DateTime.UtcNow;

            await _preferenceDal.UpdateAsync(preference);
            return new SuccessResult("Bildiriş seçimləri yeniləndi.");
        }

        private async Task<NotificationPreference> GetOrCreatePreferenceAsync(int userId)
        {
            var preference = await _preferenceDal.GetAsync(p => p.UserId == userId);
            if (preference is not null) return preference;

            preference = new NotificationPreference { UserId = userId };
            await _preferenceDal.AddAsync(preference);
            return preference;
        }

        private async Task<Dictionary<int, NotificationPreference>> LoadPreferencesAsync(List<int> userIds)
        {
            var query = await _preferenceDal.GetListAsync(p => userIds.Contains(p.UserId));
            return await query.ToDictionaryAsync(p => p.UserId);
        }

        public static string TypeLabel(NotificationType type) => type switch
        {
            NotificationType.Announcement => "Elan",
            NotificationType.ReservationCreated => "Yeni rezerv sorğusu",
            NotificationType.ReservationStatusChanged => "Rezervin cavabı",
            NotificationType.ReservationCancelled => "Rezerv ləğvi",
            NotificationType.ReservationReminder => "Xatırlatma",
            NotificationType.ReservationExpired => "Vaxtı keçdi",
            NotificationType.VenueApproved => "Saray təsdiqləndi",
            NotificationType.VenueRejected => "Saray təsdiqlənmədi",
            NotificationType.ReviewReceived => "Yeni rəy",
            _ => "Bildiriş"
        };
    }
}
