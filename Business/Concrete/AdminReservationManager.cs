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

        public AdminReservationManager(IReservationDal reservationDal, IVenueDal venueDal)
        {
            _reservationDal = reservationDal;
            _venueDal = venueDal;
        }

        public async Task<IDataResult<AdminReservationStatsDto>> GetStatsAsync()
        {
            var all = await _reservationDal.GetListAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var staleBefore = DateTime.UtcNow.AddDays(-3);

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
            var query = await _reservationDal.GetListAsync();
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

            if (dto.VenueId.HasValue)
                query = query.Where(r => r.VenueId == dto.VenueId.Value);

            if (!string.IsNullOrWhiteSpace(dto.Status)
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
                IsPast = r.EventDate < today
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

        private static string StatusLabel(ReservationStatus status) => status switch
        {
            ReservationStatus.Pending => "Gözləyir",
            ReservationStatus.Confirmed => "Təsdiqlənib",
            ReservationStatus.Rejected => "Rədd edilib",
            _ => "Ləğv edilib"
        };
    }
}
