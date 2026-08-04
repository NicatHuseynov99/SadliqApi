using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Reservation;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    // Bütün sarayların rezervlərinə vahid baxış. Statusu dəyişmək lazım gələndə
    // iş ReservationManager-ə ötürülür ki, təqvim və bildiriş məntiqi təkrarlanmasın.
    public class AdminReservationManager : IAdminReservationService
    {
        private readonly IReservationDal _reservationDal;
        private readonly IVenueDal _venueDal;
        private readonly IPlatformSettingsService _settings;

        public AdminReservationManager(
            IReservationDal reservationDal,
            IVenueDal venueDal,
            IPlatformSettingsService settings)
        {
            _reservationDal = reservationDal;
            _venueDal = venueDal;
            _settings = settings;
        }

        public async Task<IDataResult<AdminReservationStatsDto>> GetStatsAsync()
        {
            var all = await _reservationDal.GetListAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            // «Gecikən» sayılma həddi admin paneldən tənzimlənir
            var staleDays = await _settings.GetIntAsync("stale_pending_days", 3);
            var staleBefore = DateTime.UtcNow.AddDays(-staleDays);

            var stats = new AdminReservationStatsDto
            {
                Total = await all.CountAsync(),
                Pending = await all.CountAsync(r => r.Status == ReservationStatus.Pending),
                Confirmed = await all.CountAsync(r => r.Status == ReservationStatus.Confirmed),
                Rejected = await all.CountAsync(r => r.Status == ReservationStatus.Rejected),
                Cancelled = await all.CountAsync(r => r.Status == ReservationStatus.Cancelled),
                Upcoming = await all.CountAsync(r =>
                    r.Status == ReservationStatus.Confirmed && r.EventDate >= today),
                StalePending = await all.CountAsync(r =>
                    r.Status == ReservationStatus.Pending && r.CreatedAt < staleBefore)
            };

            // Ən çox sorğu alan saraylar — hansı sarayın yükü ağırdır
            var grouped = await all
                .GroupBy(r => r.VenueId)
                .Select(g => new
                {
                    VenueId = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(r => r.Status == ReservationStatus.Pending)
                })
                .OrderByDescending(g => g.Total)
                .Take(5)
                .ToListAsync();

            var ids = grouped.Select(g => g.VenueId).ToList();
            var names = await (await _venueDal.GetListAsync(v => ids.Contains(v.Id)))
                .Select(v => new { v.Id, v.Name })
                .ToDictionaryAsync(v => v.Id, v => v.Name);

            stats.TopVenues = grouped.Select(g => new VenueReservationCountDto
            {
                VenueId = g.VenueId,
                Name = names.TryGetValue(g.VenueId, out var name) ? name : "—",
                Total = g.Total,
                Pending = g.Pending
            }).ToList();

            return new SuccessDataResult<AdminReservationStatsDto>(stats);
        }

        public async Task<IDataResult<PagedResult<AdminReservationDto>>> SearchAsync(AdminReservationSearchDto dto)
        {
            // Admin silinmiş rezervləri də görə bilməlidir
            var query = (await _reservationDal.GetListAsync()).IgnoreQueryFilters();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            query = dto.Status == "Deleted"
                ? query.Where(r => r.IsDeleted)
                : query.Where(r => !r.IsDeleted);

            if (dto.VenueId.HasValue)
                query = query.Where(r => r.VenueId == dto.VenueId.Value);

            if (!string.IsNullOrWhiteSpace(dto.Status) && dto.Status != "Deleted"
                && Enum.TryParse<ReservationStatus>(dto.Status, true, out var status))
                query = query.Where(r => r.Status == status);

            if (dto.From.HasValue)
                query = query.Where(r => r.EventDate >= dto.From.Value);

            if (dto.To.HasValue)
                query = query.Where(r => r.EventDate <= dto.To.Value);

            query = dto.Period switch
            {
                "upcoming" => query.Where(r => r.EventDate >= today),
                "past" => query.Where(r => r.EventDate < today),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var q = dto.Q.Trim().ToLower();
                query = query.Where(r =>
                    r.ContactPhone.ToLower().Contains(q) ||
                    (r.Note != null && r.Note.ToLower().Contains(q)) ||
                    (r.Customer != null &&
                     (r.Customer.FirstName + " " + r.Customer.LastName).ToLower().Contains(q)) ||
                    (r.Venue != null && r.Venue.Name.ToLower().Contains(q)));
            }

            var total = await query.CountAsync();

            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 100 ? 20 : dto.PageSize;

            // Gözləyənlər öndə, sonra yaxın tarixlər — iş sırası budur
            var rows = await query
                .Include(r => r.Customer)
                .Include(r => r.Venue)
                .Include(r => r.Hall)
                .Include(r => r.Menu)
                .OrderBy(r => r.Status == ReservationStatus.Pending ? 0 : 1)
                .ThenByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rows.Select(r => new AdminReservationDto
            {
                Id = r.Id,
                VenueId = r.VenueId,
                VenueName = r.Venue?.Name ?? "—",
                VenueImageUrl = r.Venue?.CoverImageUrl,
                City = r.Venue?.City ?? string.Empty,
                HallName = r.Hall?.Name ?? "—",
                MenuName = r.Menu?.Name ?? "—",
                CustomerId = r.CustomerId,
                CustomerName = r.Customer is null ? "—" : $"{r.Customer.FirstName} {r.Customer.LastName}",
                CustomerEmail = r.Customer?.Email,
                ContactPhone = r.ContactPhone,
                EventDate = r.EventDate,
                GuestCount = r.GuestCount,
                Note = r.Note,
                Status = r.Status,
                StatusLabel = StatusLabel(r.Status),
                CreatedAt = r.CreatedAt,
                IsPast = r.EventDate < today,
                IsDeleted = r.IsDeleted,
                DeletedAt = r.DeletedAt
            }).ToList();

            var result = new PagedResult<AdminReservationDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<AdminReservationDto>>(result);
        }

        public async Task<IResult> RestoreAsync(int reservationId)
        {
            var query = (await _reservationDal.GetListAsync()).IgnoreQueryFilters();
            var reservation = await query.FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation is null)
                return new ErrorResult("Rezerv tapılmadı.");

            if (!reservation.IsDeleted)
                return new ErrorResult("Bu rezerv silinməyib.");

            reservation.IsDeleted = false;
            reservation.DeletedAt = null;
            await _reservationDal.UpdateAsync(reservation);

            return new SuccessResult("Rezerv geri qaytarıldı.");
        }

        private static string StatusLabel(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Gözləyir",
            ReservationStatus.Confirmed => "Təsdiqlənib",
            ReservationStatus.Rejected => "Rədd edilib",
            ReservationStatus.Expired => "Vaxtı keçib",
            _ => "Ləğv edilib"
        };
    }
}
