using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework.Contexts;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Dispute;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Mübahisənin həlli: iki tərəfin izahı, dəstəyin yazışması və yazılı qərar
    // bir işdə toplanır. Əvvəl admin rezervin statusunu kənardan dəyişirdi —
    // nə səbəb, nə tərəflərin sözü, nə də iz qalırdı.
    public class DisputeManager : IDisputeService
    {
        private const int MaxPageSize = 50;
        private const int MaxBodyLength = 2000;

        private readonly SadliqDbContext _context;
        private readonly IUserDal _userDal;
        private readonly IVenueAccessService _access;
        private readonly INotificationService _notifications;
        private readonly IAuditContext _audit;

        public DisputeManager(
            SadliqDbContext context,
            IUserDal userDal,
            IVenueAccessService access,
            INotificationService notifications,
            IAuditContext audit)
        {
            _context = context;
            _userDal = userDal;
            _access = access;
            _notifications = notifications;
            _audit = audit;
        }

        // ————— tərəflər —————

        public async Task<IDataResult<int>> CreateAsync(CreateDisputeDto dto, int userId)
        {
            var subject = dto.Subject?.Trim();
            var description = dto.Description?.Trim();

            if (string.IsNullOrWhiteSpace(subject))
                return new ErrorDataResult<int>("Mövzunu yazın.");

            if (string.IsNullOrWhiteSpace(description) || description.Length < 20)
                return new ErrorDataResult<int>(
                    "Nə baş verdiyini ən azı 20 simvolla izah edin — dəstək qərar verə bilməlidir.");

            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null)
                return new ErrorDataResult<int>("İstifadəçi tapılmadı.");

            int? venueId = dto.VenueId;
            Reservation? reservation = null;

            // Rezerv göstərilibsə həm sarayı ondan götürürük, həm də şikayətçinin
            // həqiqətən həmin rezervin tərəfi olduğunu yoxlayırıq
            if (dto.ReservationId is { } reservationId)
            {
                reservation = await _context.Reservations
                    .FirstOrDefaultAsync(r => r.Id == reservationId);

                if (reservation is null)
                    return new ErrorDataResult<int>("Rezerv tapılmadı.");

                var isCustomerSide = reservation.CustomerId == userId;
                var isVenueSide = await _access.CanManageAsync(reservation.VenueId, userId);

                if (!isCustomerSide && !isVenueSide)
                    return new ErrorDataResult<int>("Bu rezerv sizə aid deyil.");

                venueId = reservation.VenueId;
            }
            else if (venueId is { } directVenueId)
            {
                var venueExists = await _context.Venues.AnyAsync(v => v.Id == directVenueId);
                if (!venueExists)
                    return new ErrorDataResult<int>("Saray tapılmadı.");
            }
            else
            {
                return new ErrorDataResult<int>("Şikayət rezerv və ya saray üzrə açılmalıdır.");
            }

            // Müştəri deyilsə saray tərəfi sayılır (sahib və ya administrator)
            var openedByCustomer = user.Role == UserRole.Customer;

            // Eyni rezerv üzrə açıq iş varsa təkrar açılmasın
            if (dto.ReservationId is { } rid)
            {
                var alreadyOpen = await _context.Disputes.AnyAsync(d =>
                    d.ReservationId == rid
                    && d.OpenedByUserId == userId
                    && (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.InReview));

                if (alreadyOpen)
                    return new ErrorDataResult<int>("Bu rezerv üzrə açıq şikayətiniz artıq var.");
            }

            var dispute = new Dispute
            {
                OpenedByUserId = userId,
                OpenedByCustomer = openedByCustomer,
                ReservationId = dto.ReservationId,
                VenueId = venueId,
                Category = dto.Category,
                Subject = subject,
                Description = description,
                Status = DisputeStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();

            _audit.Set(
                "DisputeOpened",
                $"«{subject}» mövzusunda şikayət açdı",
                "Dispute", dispute.Id, venueId);

            return new SuccessDataResult<int>(dispute.Id, "Şikayətiniz qeydə alındı — dəstək baxacaq.");
        }

        public async Task<IDataResult<List<DisputeSummaryDto>>> GetMineAsync(int userId)
        {
            var venueIds = await _access.GetManagedVenueIdsAsync(userId);

            var list = await _context.Disputes
                .Where(d => d.OpenedByUserId == userId
                            || (d.VenueId != null && venueIds.Contains(d.VenueId.Value)))
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DisputeSummaryDto
                {
                    Id = d.Id,
                    Subject = d.Subject,
                    Category = d.Category,
                    Status = d.Status,
                    OpenedByCustomer = d.OpenedByCustomer,
                    OpenedByName = d.OpenedBy!.FirstName + " " + d.OpenedBy.LastName,
                    VenueName = d.Venue!.Name,
                    ReservationId = d.ReservationId,
                    AssignedToName = d.AssignedTo == null
                        ? null
                        : d.AssignedTo.FirstName + " " + d.AssignedTo.LastName,
                    // Daxili qeydlər tərəfin mesaj sayına düşməməlidir
                    MessageCount = d.Messages.Count(m => !m.IsDeleted && !m.IsInternalNote),
                    LastMessageAt = d.Messages
                        .Where(m => !m.IsDeleted && !m.IsInternalNote)
                        .Max(m => (DateTime?)m.CreatedAt),
                    CreatedAt = d.CreatedAt,
                    ResolvedAt = d.ResolvedAt
                })
                .ToListAsync();

            foreach (var item in list) Label(item);

            return new SuccessDataResult<List<DisputeSummaryDto>>(list);
        }

        // ————— hər iki tərəf —————

        public async Task<IDataResult<DisputeDetailDto>> GetAsync(int disputeId, int userId)
        {
            var dispute = await _context.Disputes
                .Include(d => d.OpenedBy)
                .Include(d => d.Venue)
                .Include(d => d.AssignedTo)
                .FirstOrDefaultAsync(d => d.Id == disputeId);

            if (dispute is null)
                return new ErrorDataResult<DisputeDetailDto>("Şikayət tapılmadı.");

            var (canSee, isStaff) = await CanAccessAsync(dispute, userId);
            if (!canSee)
                return new ErrorDataResult<DisputeDetailDto>("Bu şikayət sizə aid deyil.");

            // Daxili qeydlər yalnız platforma işçisinə görünür
            var messages = await _context.DisputeMessages
                .Where(m => m.DisputeId == disputeId && (isStaff || !m.IsInternalNote))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new DisputeMessageDto
                {
                    Id = m.Id,
                    SenderName = m.Sender == null
                        ? "—"
                        : (m.Sender.FirstName + " " + m.Sender.LastName).Trim(),
                    IsFromStaff = m.IsFromStaff,
                    IsInternalNote = m.IsInternalNote,
                    Body = m.Body,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            var detail = new DisputeDetailDto
            {
                Id = dispute.Id,
                Subject = dispute.Subject,
                Description = dispute.Description,
                Category = dispute.Category,
                Status = dispute.Status,
                OpenedByCustomer = dispute.OpenedByCustomer,
                OpenedByName = FullName(dispute.OpenedBy),
                VenueName = dispute.Venue?.Name,
                ReservationId = dispute.ReservationId,
                AssignedToName = dispute.AssignedTo is null ? null : FullName(dispute.AssignedTo),
                Resolution = dispute.Resolution,
                MessageCount = messages.Count(m => !m.IsInternalNote),
                LastMessageAt = messages.LastOrDefault()?.CreatedAt,
                CreatedAt = dispute.CreatedAt,
                ResolvedAt = dispute.ResolvedAt,
                Messages = messages
            };

            // Qarşı tərəfin adı yalnız dəstəyə lazımdır
            if (isStaff) detail.CounterpartName = await CounterpartNameAsync(dispute);

            Label(detail);

            return new SuccessDataResult<DisputeDetailDto>(detail);
        }

        public async Task<IResult> SendMessageAsync(int disputeId, SendDisputeMessageDto dto, int userId)
        {
            var body = dto.Body?.Trim();

            if (string.IsNullOrWhiteSpace(body))
                return new ErrorResult("Mesaj boş ola bilməz.");

            if (body.Length > MaxBodyLength)
                return new ErrorResult($"Mesaj {MaxBodyLength} simvoldan uzun ola bilməz.");

            var dispute = await _context.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute is null)
                return new ErrorResult("Şikayət tapılmadı.");

            var (canSee, isStaff) = await CanAccessAsync(dispute, userId);
            if (!canSee)
                return new ErrorResult("Bu şikayət sizə aid deyil.");

            if (dispute.Status is DisputeStatus.Resolved or DisputeStatus.Closed)
                return new ErrorResult("Bağlanmış iş üzrə yazışma mümkün deyil.");

            // Daxili qeyd yalnız platforma işçisinin imkanıdır
            var isInternal = dto.IsInternalNote && isStaff;

            _context.DisputeMessages.Add(new DisputeMessage
            {
                DisputeId = disputeId,
                SenderUserId = userId,
                IsFromStaff = isStaff,
                IsInternalNote = isInternal,
                Body = body,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Daxili qeyd bildiriş yaratmır
            if (!isInternal) await NotifyOthersAsync(dispute, userId, body);

            return new SuccessResult("Mesaj göndərildi.");
        }

        // ————— dəstək —————

        public async Task<IDataResult<PagedResult<DisputeSummaryDto>>> SearchAsync(DisputeQueryDto query)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var size = Math.Clamp(query.PageSize, 1, MaxPageSize);

            var disputes = _context.Disputes.AsQueryable();

            if (Enum.TryParse<DisputeStatus>(query.Status, true, out var status))
                disputes = disputes.Where(d => d.Status == status);

            if (query.Category is { } category)
                disputes = disputes.Where(d => d.Category == category);

            var search = query.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
                disputes = disputes.Where(d =>
                    d.Subject.Contains(search)
                    || d.Description.Contains(search)
                    || (d.OpenedBy!.FirstName + " " + d.OpenedBy.LastName).Contains(search)
                    || (d.Venue != null && d.Venue.Name.Contains(search)));

            var total = await disputes.CountAsync();

            // Açıq işlər yuxarıda: dəstəyin növbəsi budur
            var items = await disputes
                .OrderBy(d => d.Status == DisputeStatus.Resolved || d.Status == DisputeStatus.Closed)
                .ThenByDescending(d => d.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(d => new DisputeSummaryDto
                {
                    Id = d.Id,
                    Subject = d.Subject,
                    Category = d.Category,
                    Status = d.Status,
                    OpenedByCustomer = d.OpenedByCustomer,
                    OpenedByName = d.OpenedBy!.FirstName + " " + d.OpenedBy.LastName,
                    VenueName = d.Venue!.Name,
                    ReservationId = d.ReservationId,
                    AssignedToName = d.AssignedTo == null
                        ? null
                        : d.AssignedTo.FirstName + " " + d.AssignedTo.LastName,
                    MessageCount = d.Messages.Count(m => !m.IsDeleted),
                    LastMessageAt = d.Messages
                        .Where(m => !m.IsDeleted)
                        .Max(m => (DateTime?)m.CreatedAt),
                    CreatedAt = d.CreatedAt,
                    ResolvedAt = d.ResolvedAt
                })
                .ToListAsync();

            foreach (var item in items) Label(item);

            return new SuccessDataResult<PagedResult<DisputeSummaryDto>>(new PagedResult<DisputeSummaryDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = size
            });
        }

        public async Task<IResult> AssignAsync(int disputeId, int staffUserId)
        {
            var dispute = await _context.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute is null)
                return new ErrorResult("Şikayət tapılmadı.");

            if (dispute.Status is DisputeStatus.Resolved or DisputeStatus.Closed)
                return new ErrorResult("Bağlanmış iş yenidən götürülə bilməz.");

            dispute.AssignedToUserId = staffUserId;
            if (dispute.Status == DisputeStatus.Open) dispute.Status = DisputeStatus.InReview;

            await _context.SaveChangesAsync();

            // Tərəflər işə baxıldığını bilməlidir — gözləmək pisdir,
            // baxılıb-baxılmadığını bilməmək daha pisdir
            await NotifyPartiesAsync(dispute,
                "Şikayətinizə baxılır",
                $"«{dispute.Subject}» üzrə iş dəstək əməkdaşına təyin olundu.");

            return new SuccessResult("İş sizin üzərinizdədir.");
        }

        public async Task<IResult> ResolveAsync(int disputeId, ResolveDisputeDto dto, int staffUserId)
        {
            var resolution = dto.Resolution?.Trim();

            if (string.IsNullOrWhiteSpace(resolution))
                return new ErrorResult("Qərarın izahını yazın — tərəflər bunu görəcək.");

            if (dto.Status is not (DisputeStatus.Resolved or DisputeStatus.Closed))
                return new ErrorResult("İş yalnız «həll edildi» və ya «bağlandı» ola bilər.");

            var dispute = await _context.Disputes.FirstOrDefaultAsync(d => d.Id == disputeId);
            if (dispute is null)
                return new ErrorResult("Şikayət tapılmadı.");

            if (dispute.Status is DisputeStatus.Resolved or DisputeStatus.Closed)
                return new ErrorResult("Bu iş artıq bağlanıb.");

            dispute.Status = dto.Status;
            dispute.Resolution = resolution;
            dispute.ResolvedAt = DateTime.UtcNow;
            dispute.ResolvedByUserId = staffUserId;
            dispute.AssignedToUserId ??= staffUserId;

            await _context.SaveChangesAsync();

            _audit.Set(
                "DisputeResolved",
                $"«{dispute.Subject}» şikayətini bağladı — {resolution}",
                "Dispute", dispute.Id, dispute.VenueId);

            await NotifyPartiesAsync(dispute,
                dto.Status == DisputeStatus.Resolved ? "Şikayətiniz həll edildi" : "Şikayətiniz bağlandı",
                resolution);

            return new SuccessResult("İş bağlandı və tərəflərə bildirildi.");
        }

        // ————— köməkçilər —————

        // İşi kim görə bilər: açan, qarşı tərəf (sarayın adamı) və platforma işçisi
        private async Task<(bool CanSee, bool IsStaff)> CanAccessAsync(Dispute dispute, int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            if (user is null) return (false, false);

            var isStaff = user.Role is UserRole.SuperAdmin or UserRole.Support;
            if (isStaff) return (true, true);

            if (dispute.OpenedByUserId == userId) return (true, false);

            // Şikayət saray üzrədirsə, sarayın adamı da görməlidir —
            // özünü müdafiə etmək imkanı olmalıdır
            if (dispute.VenueId is { } venueId && await _access.CanManageAsync(venueId, userId))
                return (true, false);

            // Rezerv üzrə şikayətdə müştəri də tərəfdir
            if (dispute.ReservationId is { } reservationId)
            {
                var isCustomer = await _context.Reservations
                    .AnyAsync(r => r.Id == reservationId && r.CustomerId == userId);

                if (isCustomer) return (true, false);
            }

            return (false, false);
        }

        // Qarşı tərəfin adı: müştəri açıbsa sarayın sahibi, saray açıbsa müştəri
        private async Task<string?> CounterpartNameAsync(Dispute dispute)
        {
            if (dispute.OpenedByCustomer)
            {
                if (dispute.VenueId is not { } venueId) return null;

                var ownerId = await _context.Venues
                    .Where(v => v.Id == venueId)
                    .Select(v => v.OwnerId)
                    .FirstOrDefaultAsync();

                var owner = await _userDal.GetAsync(u => u.Id == ownerId);
                return owner is null ? null : FullName(owner);
            }

            if (dispute.ReservationId is not { } reservationId) return null;

            var customerId = await _context.Reservations
                .Where(r => r.Id == reservationId)
                .Select(r => r.CustomerId)
                .FirstOrDefaultAsync();

            var customer = await _userDal.GetAsync(u => u.Id == customerId);
            return customer is null ? null : FullName(customer);
        }

        // İşin tərəflərinə bildiriş: açan və sarayın sahibi
        private async Task NotifyPartiesAsync(Dispute dispute, string title, string message)
        {
            var targets = new HashSet<int> { dispute.OpenedByUserId };

            if (dispute.VenueId is { } venueId)
            {
                var ownerId = await _context.Venues
                    .Where(v => v.Id == venueId)
                    .Select(v => v.OwnerId)
                    .FirstOrDefaultAsync();

                if (ownerId != 0) targets.Add(ownerId);
            }

            if (dispute.ReservationId is { } reservationId)
            {
                var customerId = await _context.Reservations
                    .Where(r => r.Id == reservationId)
                    .Select(r => r.CustomerId)
                    .FirstOrDefaultAsync();

                if (customerId != 0) targets.Add(customerId);
            }

            foreach (var target in targets)
                await _notifications.NotifyAsync(
                    target,
                    new NotificationMessage(
                        NotificationType.SystemMessage, title, message, "/sikayetlerim"));
        }

        // Yeni mesajdan digər iştirakçılar xəbər tutmalıdır
        private async Task NotifyOthersAsync(Dispute dispute, int senderId, string body)
        {
            var preview = body.Length > 120 ? body[..120] + "…" : body;

            var targets = new HashSet<int> { dispute.OpenedByUserId };

            if (dispute.VenueId is { } venueId)
            {
                var ownerId = await _context.Venues
                    .Where(v => v.Id == venueId)
                    .Select(v => v.OwnerId)
                    .FirstOrDefaultAsync();

                if (ownerId != 0) targets.Add(ownerId);
            }

            if (dispute.AssignedToUserId is { } assignee) targets.Add(assignee);

            targets.Remove(senderId);

            foreach (var target in targets)
                await _notifications.NotifyAsync(
                    target,
                    new NotificationMessage(
                        NotificationType.SystemMessage,
                        $"Şikayət üzrə yeni mesaj — {dispute.Subject}",
                        preview,
                        "/sikayetlerim"));
        }

        private static string FullName(User? user) =>
            user is null ? "—" : $"{user.FirstName} {user.LastName}".Trim();

        private static void Label(DisputeSummaryDto dto)
        {
            dto.StatusLabel = dto.Status switch
            {
                DisputeStatus.Open => "Açıq",
                DisputeStatus.InReview => "Baxılır",
                DisputeStatus.Resolved => "Həll edildi",
                _ => "Bağlandı"
            };

            dto.CategoryLabel = dto.Category switch
            {
                DisputeCategory.Reservation => "Rezerv",
                DisputeCategory.VenueBehavior => "Sarayın davranışı",
                DisputeCategory.CustomerBehavior => "Müştərinin davranışı",
                DisputeCategory.Payment => "Qiymət və ödəniş",
                _ => "Digər"
            };
        }
    }
}
