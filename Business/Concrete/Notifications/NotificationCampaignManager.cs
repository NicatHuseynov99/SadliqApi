using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Notification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Business.Concrete.Notifications
{
    public class NotificationCampaignManager : INotificationCampaignService
    {
        private readonly INotificationCampaignDal _campaignDal;
        private readonly IAudienceDal _audienceDal;
        private readonly INotificationService _notifications;
        private readonly IUserDal _userDal;
        private readonly IVenueDal _venueDal;
        private readonly IAuditContext _audit;
        private readonly ILogger<NotificationCampaignManager> _logger;

        public NotificationCampaignManager(
            INotificationCampaignDal campaignDal,
            IAudienceDal audienceDal,
            INotificationService notifications,
            IUserDal userDal,
            IVenueDal venueDal,
            IAuditContext audit,
            ILogger<NotificationCampaignManager> logger)
        {
            _campaignDal = campaignDal;
            _audienceDal = audienceDal;
            _notifications = notifications;
            _userDal = userDal;
            _venueDal = venueDal;
            _audit = audit;
            _logger = logger;
        }

        // ————— auditoriya —————

        public async Task<IDataResult<AudiencePreviewDto>> PreviewAsync(AudienceDto audience, int requestedByUserId)
        {
            var allowed = await LimitToOwnVenueAsync(audience, requestedByUserId);
            if (!allowed.Success)
                return new ErrorDataResult<AudiencePreviewDto>(allowed.Message);

            var ids = await _audienceDal.ResolveAsync(audience);

            return new SuccessDataResult<AudiencePreviewDto>(new AudiencePreviewDto
            {
                Count = ids.Count,
                Summary = await DescribeAsync(audience),
                Sample = await _audienceDal.GetSampleNamesAsync(ids, 5)
            });
        }

        // Saray sahibi yalnız öz qonaqlarına yaza bilər — platformanın bütün
        // istifadəçilərinə çıxışı yoxdur
        private async Task<IResult> LimitToOwnVenueAsync(AudienceDto audience, int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null) return new ErrorResult("İstifadəçi tapılmadı.");

            if (user.Role is UserRole.SuperAdmin or UserRole.Support)
                return new SuccessResult();

            if (audience.VenueId is not int venueId)
                return new ErrorResult("Yalnız öz sarayınızın qonaqlarına bildiriş göndərə bilərsiniz.");

            var venue = await _venueDal.GetAsync(v => v.Id == venueId);
            if (venue is null || venue.OwnerId != userId)
                return new ErrorResult("Bu saray sizə aid deyil.");

            return new SuccessResult();
        }

        // ————— göndərmə —————

        public async Task<IDataResult<CampaignDto>> SendAsync(CreateCampaignDto dto, int senderUserId)
        {
            var validation = Validate(dto);
            if (validation is not null)
                return new ErrorDataResult<CampaignDto>(validation);

            var allowed = await LimitToOwnVenueAsync(dto.Audience, senderUserId);
            if (!allowed.Success)
                return new ErrorDataResult<CampaignDto>(allowed.Message);

            var campaign = new NotificationCampaign
            {
                CreatedByUserId = senderUserId,
                Title = dto.Title.Trim(),
                Body = dto.Body.Trim(),
                Link = string.IsNullOrWhiteSpace(dto.Link) ? null : dto.Link.Trim(),
                AudienceJson = JsonSerializer.Serialize(dto.Audience),
                AudienceSummary = await DescribeAsync(dto.Audience),
                Channels = dto.Channels == NotificationChannels.None
                    ? NotificationChannels.InApp
                    : dto.Channels,
                ScheduledAt = dto.ScheduledAt,
                Status = dto.ScheduledAt.HasValue && dto.ScheduledAt > DateTime.UtcNow
                    ? CampaignStatus.Scheduled
                    : CampaignStatus.Draft
            };

            await _campaignDal.AddAsync(campaign);

            // Vaxt göstərilməyibsə dərhal göndərilir
            if (campaign.Status == CampaignStatus.Draft)
                await DeliverAsync(campaign);

            _audit.Set(
                "NotificationCampaignSent",
                $"«{campaign.Title}» bildirişini göndərdi ({campaign.AudienceSummary})",
                "NotificationCampaign", campaign.Id, null);

            return new SuccessDataResult<CampaignDto>(
                ToDto(campaign, string.Empty),
                campaign.Status == CampaignStatus.Scheduled
                    ? "Bildiriş planlaşdırıldı."
                    : $"Bildiriş {campaign.RecipientCount} nəfərə göndərildi.");
        }

        public async Task<IResult> SendTestAsync(CreateCampaignDto dto, int senderUserId)
        {
            var validation = Validate(dto);
            if (validation is not null) return new ErrorResult(validation);

            await _notifications.NotifyAsync(
                senderUserId,
                new NotificationMessage(
                    NotificationType.Announcement,
                    $"[SINAQ] {dto.Title.Trim()}",
                    dto.Body.Trim(),
                    dto.Link),
                dto.Channels == NotificationChannels.None ? NotificationChannels.InApp : dto.Channels);

            return new SuccessResult("Sınaq bildirişi sizə göndərildi.");
        }

        // Elanın həqiqi göndərilməsi
        private async Task DeliverAsync(NotificationCampaign campaign)
        {
            campaign.Status = CampaignStatus.Sending;
            await _campaignDal.UpdateAsync(campaign);

            try
            {
                var audience = JsonSerializer.Deserialize<AudienceDto>(campaign.AudienceJson) ?? new AudienceDto();
                var ids = await _audienceDal.ResolveAsync(audience);

                await _notifications.NotifyManyAsync(
                    ids,
                    new NotificationMessage(
                        NotificationType.Announcement,
                        campaign.Title,
                        campaign.Body,
                        campaign.Link,
                        campaign.Id),
                    campaign.Channels);

                campaign.RecipientCount = ids.Count;
                campaign.SentAt = DateTime.UtcNow;
                campaign.Status = CampaignStatus.Sent;
                campaign.Error = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bildiriş göndərilmədi: {Id}", campaign.Id);
                campaign.Status = CampaignStatus.Failed;
                campaign.Error = ex.Message;
            }

            await _campaignDal.UpdateAsync(campaign);
        }

        // Arxa fon xidməti bunu çağırır
        public async Task<int> DispatchDueAsync(CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            var query = await _campaignDal.GetListAsync(
                c => c.Status == CampaignStatus.Scheduled && c.ScheduledAt != null && c.ScheduledAt <= now);

            var due = await query.ToListAsync(token);

            foreach (var campaign in due)
            {
                // AsNoTracking ilə gəldiyi üçün yenidən oxunur
                var tracked = await _campaignDal.GetAsync(c => c.Id == campaign.Id);
                if (tracked is null) continue;

                await DeliverAsync(tracked);
            }

            return due.Count;
        }

        // ————— siyahı —————

        public async Task<IDataResult<PagedResult<CampaignDto>>> SearchAsync(CampaignQueryDto query)
        {
            var page = Math.Max(1, query.Page);
            var size = Math.Clamp(query.PageSize, 1, 100);

            var all = await _campaignDal.GetListAsync();

            if (!string.IsNullOrWhiteSpace(query.Status)
                && Enum.TryParse<CampaignStatus>(query.Status, true, out var status))
                all = all.Where(c => c.Status == status);

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim().ToLower();
                all = all.Where(c => c.Title.ToLower().Contains(term) || c.Body.ToLower().Contains(term));
            }

            var total = await all.CountAsync();

            var items = await all
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(c => new
                {
                    Campaign = c,
                    SenderName = c.CreatedBy!.FirstName + " " + c.CreatedBy.LastName
                })
                .ToListAsync();

            return new SuccessDataResult<PagedResult<CampaignDto>>(new PagedResult<CampaignDto>
            {
                Items = items.Select(x => ToDto(x.Campaign, x.SenderName)).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = size
            });
        }

        public async Task<IResult> CancelAsync(int campaignId)
        {
            var campaign = await _campaignDal.GetAsync(c => c.Id == campaignId);
            if (campaign is null) return new ErrorResult("Bildiriş tapılmadı.");

            if (campaign.Status != CampaignStatus.Scheduled)
                return new ErrorResult("Yalnız planlaşdırılmış göndərişi ləğv etmək olar.");

            campaign.Status = CampaignStatus.Cancelled;
            await _campaignDal.UpdateAsync(campaign);

            return new SuccessResult("Göndəriş ləğv edildi.");
        }

        // ————— köməkçilər —————

        private static string? Validate(CreateCampaignDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length < 3)
                return "Başlıq ən azı 3 simvol olmalıdır.";

            if (dto.Title.Trim().Length > 120)
                return "Başlıq 120 simvoldan uzun ola bilməz.";

            if (string.IsNullOrWhiteSpace(dto.Body) || dto.Body.Trim().Length < 10)
                return "Mətn ən azı 10 simvol olmalıdır.";

            if (dto.Body.Trim().Length > 2000)
                return "Mətn 2000 simvoldan uzun ola bilməz.";

            if (dto.ScheduledAt.HasValue && dto.ScheduledAt < DateTime.UtcNow.AddMinutes(-1))
                return "Göndəriş tarixi keçmişdə ola bilməz.";

            return null;
        }

        // İnsan dilində auditoriya izahı — göndərişdən sonra da başa düşülsün
        private async Task<string> DescribeAsync(AudienceDto audience)
        {
            var parts = new List<string>();

            if (audience.UserIds.Count > 0)
                return $"Seçilmiş {audience.UserIds.Count} istifadəçi";

            parts.Add(audience.Roles.Count == 0
                ? "Bütün istifadəçilər"
                : string.Join(", ", audience.Roles.Select(RoleLabel)));

            if (audience.Cities.Count > 0)
                parts.Add(string.Join(", ", audience.Cities));

            if (audience.VenueId is int venueId)
            {
                var venue = await _venueDal.GetAsync(v => v.Id == venueId);
                parts.Add($"«{venue?.Name ?? "saray"}» qonaqları");
            }

            if (audience.Behavior != AudienceBehavior.Any)
                parts.Add(BehaviorLabel(audience.Behavior, audience.BehaviorDays));

            return string.Join(" · ", parts);
        }

        private static string RoleLabel(UserRole role) => role switch
        {
            UserRole.Customer => "Müştərilər",
            UserRole.Owner => "Saray sahibləri",
            UserRole.VenueAdmin => "Saray administratorları",
            UserRole.Support => "Dəstək",
            _ => "Adminlər"
        };

        private static string BehaviorLabel(AudienceBehavior behavior, int days) => behavior switch
        {
            AudienceBehavior.ReservedRecently => $"son {days} gündə sorğu göndərənlər",
            AudienceBehavior.NeverReserved => "heç vaxt sorğu göndərməyənlər",
            AudienceBehavior.FavoritedNotReserved => "bəyənib, amma sorğu göndərməyənlər",
            AudienceBehavior.ReservationRejected => $"son {days} gündə sorğusu alınmayanlar",
            AudienceBehavior.Inactive => $"{days} gündür girməyənlər",
            _ => string.Empty
        };

        private static string StatusLabel(CampaignStatus status) => status switch
        {
            CampaignStatus.Draft => "Qaralama",
            CampaignStatus.Scheduled => "Planlaşdırılıb",
            CampaignStatus.Sending => "Göndərilir",
            CampaignStatus.Sent => "Göndərilib",
            CampaignStatus.Failed => "Alınmadı",
            _ => "Ləğv edilib"
        };

        private static string ChannelsLabel(NotificationChannels channels)
        {
            var names = new List<string>();
            if (channels.HasFlag(NotificationChannels.InApp)) names.Add("Saytda");
            if (channels.HasFlag(NotificationChannels.Email)) names.Add("E-poçt");
            if (channels.HasFlag(NotificationChannels.Sms)) names.Add("SMS");
            return names.Count == 0 ? "—" : string.Join(" + ", names);
        }

        private static CampaignDto ToDto(NotificationCampaign c, string senderName) => new()
        {
            Id = c.Id,
            Title = c.Title,
            Body = c.Body,
            Link = c.Link,
            AudienceSummary = c.AudienceSummary,
            ChannelsLabel = ChannelsLabel(c.Channels),
            Status = c.Status,
            StatusLabel = StatusLabel(c.Status),
            ScheduledAt = c.ScheduledAt,
            SentAt = c.SentAt,
            CreatedAt = c.CreatedAt,
            CreatedByName = senderName,
            RecipientCount = c.RecipientCount,
            ReadCount = c.ReadCount,
            ClickCount = c.ClickCount,
            Error = c.Error
        };
    }
}
