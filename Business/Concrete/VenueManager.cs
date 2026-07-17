using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Venue;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueManager : IVenueService
    {
        private readonly IVenueDal _venueDal;

        public VenueManager(IVenueDal venueDal)
        {
            _venueDal = venueDal;
        }

        // Yalnız admin tərəfindən təsdiqlənmiş sarayları, şəkil/zal/menyuları ilə birgə qaytarır
        public async Task<IDataResult<List<Venue>>> GetAllAsync()
        {
            var query = await _venueDal.GetListAsync(v => v.IsApproved);
            var venues = await query
                .Include(v => v.Images)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .ToListAsync();

            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Tək sarayın bütün detallarını (şəkillər, zallar+media+dolu günlər, menyular+tərkib) qaytarır
        public async Task<IDataResult<Venue>> GetByIdAsync(int id)
        {
            var query = await _venueDal.GetListAsync(v => v.Id == id);
            var venue = await query
                .Include(v => v.Images)
                .Include(v => v.Halls).ThenInclude(h => h.Media)
                .Include(v => v.Halls).ThenInclude(h => h.Availability)
                .Include(v => v.Menus).ThenInclude(m => m.Items)
                .FirstOrDefaultAsync();

            return venue is null
                ? new ErrorDataResult<Venue>("Saray tapılmadı.")
                : new SuccessDataResult<Venue>(venue);
        }

        // Yeni sadlıq sarayı əlavə edir, təsdiqə qədər IsApproved=false qalır
        public async Task<IDataResult<Venue>> CreateAsync(CreateVenueDto dto, int ownerId)
        {
            var venue = new Venue
            {
                OwnerId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                City = dto.City,
                Address = dto.Address,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Phone = dto.Phone,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            await _venueDal.AddAsync(venue);
            return new SuccessDataResult<Venue>(venue, "Saray əlavə olundu, admin təsdiqi gözlənilir.");
        }
    }
}
