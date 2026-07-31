using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Visits;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VisitManager : IVisitService
    {
        private readonly IPageViewDal _pageViewDal;
        private readonly IVenueDal _venueDal;
        private readonly IUserDal _userDal;
        private readonly IVisitWriter _writer;
        private readonly ITextCatalog _texts;

        public VisitManager(
            IPageViewDal pageViewDal,
            IVenueDal venueDal,
            IUserDal userDal,
            IVisitWriter writer,
            ITextCatalog texts)
        {
            _pageViewDal = pageViewDal;
            _venueDal = venueDal;
            _userDal = userDal;
            _writer = writer;
            _texts = texts;
        }

        // Səhifə baxışını növbəyə atır. Sessiya ID-si olmadan yazmırıq —
        // əks halda eyni ziyarətçini saymaq mümkün olmur.
        public Task<IResult> TrackAsync(TrackPageDto dto, int? userId, string? ipAddress, string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(dto.SessionId) || string.IsNullOrWhiteSpace(dto.Path))
                return Task.FromResult<IResult>(new ErrorResult("Ziyarət məlumatı natamamdır."));

            var view = new PageView
            {
                SessionId = Trim(dto.SessionId, 64)!,
                UserId = userId,
                PageType = Trim(dto.PageType, 40) ?? "other",
                Path = Trim(dto.Path, 300)!,
                VenueId = dto.VenueId,
                Title = Trim(dto.Title, 200),
                Referrer = Trim(dto.Referrer, 300),
                IpAddress = ipAddress,
                UserAgent = Trim(userAgent, 400),
                CreatedAt = DateTime.UtcNow
            };

            _writer.Enqueue(view);
            return Task.FromResult<IResult>(new SuccessResult());
        }

        // Admin panelin «Ziyarətçilər» bölməsinin yuxarı hissəsi
        public async Task<IDataResult<VisitStatsDto>> GetStatsAsync(int days = 14)
        {
            if (days < 1) days = 1;
            if (days > 90) days = 90;

            var since = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var today = DateTime.UtcNow.Date;

            var all = await _pageViewDal.GetListAsync();
            var period = all.Where(p => p.CreatedAt >= since);

            var stats = new VisitStatsDto
            {
                TotalViews = await period.CountAsync(),
                UniqueVisitors = await period.Select(p => p.SessionId).Distinct().CountAsync(),
                GuestVisitors = await period.Where(p => p.UserId == null)
                    .Select(p => p.SessionId).Distinct().CountAsync(),
                TodayViews = await all.CountAsync(p => p.CreatedAt >= today),
                TodayVisitors = await all.Where(p => p.CreatedAt >= today)
                    .Select(p => p.SessionId).Distinct().CountAsync()
            };

            // Gündəlik mənzərə. Baxışı olmayan günlər də sıfırla göstərilir ki, qrafik kəsilməsin
            var grouped = await period
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Views = g.Count(),
                    Visitors = g.Select(x => x.SessionId).Distinct().Count()
                })
                .ToListAsync();

            stats.Daily = Enumerable.Range(0, days)
                .Select(i => since.AddDays(i))
                .Select(date =>
                {
                    var row = grouped.FirstOrDefault(g => g.Date == date);
                    return new DailyVisitDto
                    {
                        Date = date,
                        Views = row?.Views ?? 0,
                        Visitors = row?.Visitors ?? 0
                    };
                })
                .ToList();

            // Ən çox baxılan saraylar
            var venueRows = await period
                .Where(p => p.VenueId != null)
                .GroupBy(p => p.VenueId!.Value)
                .Select(g => new
                {
                    VenueId = g.Key,
                    Views = g.Count(),
                    Visitors = g.Select(x => x.SessionId).Distinct().Count()
                })
                .OrderByDescending(g => g.Views)
                .Take(10)
                .ToListAsync();

            var venueIds = venueRows.Select(v => v.VenueId).ToList();
            var venueQuery = await _venueDal.GetListAsync();
            var venueNames = await venueQuery
                .Where(v => venueIds.Contains(v.Id))
                .Select(v => new { v.Id, v.Name })
                .ToListAsync();

            stats.TopVenues = venueRows.Select(v => new TopVenueDto
            {
                VenueId = v.VenueId,
                Name = venueNames.FirstOrDefault(n => n.Id == v.VenueId)?.Name ?? $"Saray #{v.VenueId}",
                Views = v.Views,
                Visitors = v.Visitors
            }).ToList();

            // Səhifə növləri üzrə bölgü
            var pageRows = await period
                .GroupBy(p => p.PageType)
                .Select(g => new { PageType = g.Key, Views = g.Count() })
                .OrderByDescending(g => g.Views)
                .ToListAsync();

            stats.TopPages = pageRows.Select(p => new TopPageDto
            {
                PageType = p.PageType,
                Label = PageLabel(p.PageType),
                Views = p.Views
            }).ToList();

            return new SuccessDataResult<VisitStatsDto>(stats);
        }

        // Ziyarət siyahısı — süzgəclər və səhifələmə ilə
        public async Task<IDataResult<PagedResult<VisitRowDto>>> SearchAsync(VisitSearchDto dto)
        {
            var query = await _pageViewDal.GetListAsync();

            if (!string.IsNullOrWhiteSpace(dto.PageType))
                query = query.Where(p => p.PageType == dto.PageType);

            if (dto.Audience == "guest")
                query = query.Where(p => p.UserId == null);
            else if (dto.Audience == "user")
                query = query.Where(p => p.UserId != null);

            if (dto.From.HasValue)
                query = query.Where(p => p.CreatedAt >= dto.From.Value.Date);

            if (dto.To.HasValue)
                query = query.Where(p => p.CreatedAt < dto.To.Value.Date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var term = dto.Q.Trim().ToLower();
                query = query.Where(p =>
                    p.Path.ToLower().Contains(term) ||
                    (p.Title != null && p.Title.ToLower().Contains(term)) ||
                    (p.IpAddress != null && p.IpAddress.Contains(term)) ||
                    p.SessionId.Contains(term));
            }

            var total = await query.CountAsync();

            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 200 ? 30 : dto.PageSize;

            var rows = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Giriş etmiş ziyarətçilərin adlarını bir sorğu ilə gətiririk
            var userIds = rows.Where(r => r.UserId != null).Select(r => r.UserId!.Value).Distinct().ToList();
            var userQuery = await _userDal.GetListAsync();
            var users = await userQuery
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var items = rows.Select(r =>
            {
                var user = users.FirstOrDefault(u => u.Id == r.UserId);
                return new VisitRowDto
                {
                    Id = r.Id,
                    SessionId = r.SessionId,
                    UserId = r.UserId,
                    UserFullName = user is null ? null : $"{user.FirstName} {user.LastName}".Trim(),
                    PageType = r.PageType,
                    PageLabel = PageLabel(r.PageType),
                    Path = r.Path,
                    VenueId = r.VenueId,
                    Title = r.Title,
                    Referrer = r.Referrer,
                    IpAddress = r.IpAddress,
                    UserAgent = r.UserAgent,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();

            var result = new PagedResult<VisitRowDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<VisitRowDto>>(result);
        }

        // Səhifə növlərinin Azərbaycanca adları
        // Səhifə adları bazadadır ("page_type" kateqoriyası) — admin paneldən dəyişilir
        private string PageLabel(string pageType) => _texts.Get(TextCategories.PageType, pageType);

        private static string? Trim(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length > max ? value[..max] : value;
        }
    }
}
