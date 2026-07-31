using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Venue;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class AdminVenueManager : IAdminVenueService
    {
        private readonly IVenueDal _venueDal;
        private readonly IUserDal _userDal;
        private readonly IReservationDal _reservationDal;
        private readonly IAuditContext _audit;

        public AdminVenueManager(
            IVenueDal venueDal,
            IUserDal userDal,
            IReservationDal reservationDal,
            IAuditContext audit)
        {
            _venueDal = venueDal;
            _userDal = userDal;
            _reservationDal = reservationDal;
            _audit = audit;
        }

        public async Task<IDataResult<PagedResult<AdminVenueDto>>> SearchAsync(AdminVenueSearchDto dto)
        {
            var query = await _venueDal.GetListAsync();

            query = dto.Status switch
            {
                "approved" => query.Where(v => v.IsApproved),
                "pending" => query.Where(v => !v.IsApproved),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(dto.City))
                query = query.Where(v => v.City == dto.City);

            if (dto.OwnerId.HasValue)
                query = query.Where(v => v.OwnerId == dto.OwnerId.Value);

            if (!string.IsNullOrWhiteSpace(dto.Q))
            {
                var q = dto.Q.Trim().ToLower();
                query = query.Where(v =>
                    v.Name.ToLower().Contains(q) ||
                    v.City.ToLower().Contains(q) ||
                    v.Address.ToLower().Contains(q) ||
                    v.Phone.Contains(q));
            }

            var total = await query.CountAsync();

            var page = dto.Page < 1 ? 1 : dto.Page;
            var pageSize = dto.PageSize is < 1 or > 100 ? 20 : dto.PageSize;

            // Təsdiq gözləyənlər həmişə öndə — iş sırası budur
            query = dto.Sort switch
            {
                "name" => query.OrderBy(v => v.IsApproved).ThenBy(v => v.Name),
                "rating" => query.OrderBy(v => v.IsApproved)
                                 .ThenByDescending(v => v.Reviews.Average(r => (double?)r.Rating) ?? 0),
                _ => query.OrderBy(v => v.IsApproved).ThenByDescending(v => v.CreatedAt)
            };

            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    Venue = v,
                    HallCount = v.Halls.Count,
                    MenuCount = v.Menus.Count,
                    ImageCount = v.Images.Count,
                    FeatureCount = v.Features.Count,
                    Features = v.Features.Select(f => f.Name).ToList(),
                    ReviewCount = v.Reviews.Count,
                    Rating = v.Reviews.Average(r => (double?)r.Rating) ?? 0
                })
                .ToListAsync();

            var venueIds = rows.Select(r => r.Venue.Id).ToList();
            var ownerIds = rows.Select(r => r.Venue.OwnerId).Distinct().ToList();

            var owners = await (await _userDal.GetListAsync(u => ownerIds.Contains(u.Id)))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToListAsync();

            var reservations = await (await _reservationDal.GetListAsync(r => venueIds.Contains(r.VenueId)))
                .GroupBy(r => r.VenueId)
                .Select(g => new
                {
                    VenueId = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(r => r.Status == ReservationStatus.Pending)
                })
                .ToListAsync();

            var items = rows.Select(r =>
            {
                var owner = owners.FirstOrDefault(o => o.Id == r.Venue.OwnerId);
                var res = reservations.FirstOrDefault(x => x.VenueId == r.Venue.Id);

                return new AdminVenueDto
                {
                    Id = r.Venue.Id,
                    Name = r.Venue.Name,
                    Description = r.Venue.Description,
                    City = r.Venue.City,
                    Address = r.Venue.Address,
                    Phone = r.Venue.Phone,
                    CoverImageUrl = r.Venue.CoverImageUrl,
                    IsApproved = r.Venue.IsApproved,
                    CreatedAt = r.Venue.CreatedAt,
                    OwnerId = r.Venue.OwnerId,
                    OwnerName = owner is null ? "—" : $"{owner.FirstName} {owner.LastName}",
                    OwnerEmail = owner?.Email,
                    HallCount = r.HallCount,
                    MenuCount = r.MenuCount,
                    ImageCount = r.ImageCount,
                    FeatureCount = r.FeatureCount,
                    Features = r.Features,
                    ReviewCount = r.ReviewCount,
                    Rating = Math.Round(r.Rating, 2),
                    ReservationCount = res?.Total ?? 0,
                    PendingReservationCount = res?.Pending ?? 0
                };
            }).ToList();

            var result = new PagedResult<AdminVenueDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };

            return new SuccessDataResult<PagedResult<AdminVenueDto>>(result);
        }

        public async Task<IResult> SetApprovedAsync(int venueId, SetVenueApprovedDto dto)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı.");

            if (venue.IsApproved == dto.IsApproved)
                return new ErrorResult(dto.IsApproved ? "Saray artıq təsdiqlidir." : "Saray onsuz da təsdiqli deyil.");

            venue.IsApproved = dto.IsApproved;
            await _venueDal.UpdateAsync(venue);

            _audit.Set(
                dto.IsApproved ? "VenueApproved" : "VenueUnapproved",
                dto.IsApproved
                    ? $"«{venue.Name}» sarayını təsdiqlədi"
                    : $"«{venue.Name}» sarayının təsdiqini geri aldı",
                "Venue", venue.Id, venue.Id);

            return new SuccessResult(dto.IsApproved
                ? "Saray təsdiqləndi və saytda göründü."
                : "Təsdiq geri alındı — saray saytda görünmür.");
        }

        public async Task<IResult> ChangeOwnerAsync(int venueId, ChangeVenueOwnerDto dto)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId);
            if (venue is null)
                return new ErrorResult("Saray tapılmadı.");

            if (venue.OwnerId == dto.NewOwnerId)
                return new ErrorResult("Saray onsuz da bu istifadəçiyə aiddir.");

            var newOwner = await _userDal.GetAsync(u => u.Id == dto.NewOwnerId);
            if (newOwner is null)
                return new ErrorResult("İstifadəçi tapılmadı.");

            // Müştəri hesabı saray idarə edə bilməz — rolu da eyni anda qaldırırıq
            if (newOwner.Role == UserRole.Customer)
            {
                newOwner.Role = UserRole.Owner;
                await _userDal.UpdateAsync(newOwner);
            }

            venue.OwnerId = dto.NewOwnerId;
            await _venueDal.UpdateAsync(venue);

            _audit.Set(
                "VenueOwnerChanged",
                $"«{venue.Name}» sarayını {newOwner.FullName} adlı istifadəçiyə keçirdi",
                "Venue", venue.Id, venue.Id);

            return new SuccessResult($"Saray {newOwner.FullName} adına keçirildi.");
        }
    }
}
