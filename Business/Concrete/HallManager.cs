using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Hall;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class HallManager : IHallService
    {
        private readonly IHallDal _hallDal;
        private readonly IVenueDal _venueDal;
        private readonly IHallAvailabilityDal _availabilityDal;
        private readonly IVenueAccessService _access;

        public HallManager(
            IHallDal hallDal,
            IVenueDal venueDal,
            IHallAvailabilityDal availabilityDal,
            IVenueAccessService access)
        {
            _hallDal = hallDal;
            _venueDal = venueDal;
            _availabilityDal = availabilityDal;
            _access = access;
        }

        // Sarayın bütün zallarını, şəkil/video və dolu-gün cədvəli ilə birgə qaytarır
        public async Task<IDataResult<List<Hall>>> GetByVenueIdAsync(int venueId)
        {
            var query = await _hallDal.GetListAsync(h => h.VenueId == venueId);
            var halls = await query
                .Include(h => h.Media)
                .Include(h => h.Availability)
                .ToListAsync();

            return new SuccessDataResult<List<Hall>>(halls);
        }

        // Tək zalın detalları
        public async Task<IDataResult<Hall>> GetByIdAsync(int id)
        {
            var query = await _hallDal.GetListAsync(h => h.Id == id);
            var hall = await query
                .Include(h => h.Media)
                .Include(h => h.Availability)
                .FirstOrDefaultAsync();

            return hall is null
                ? new ErrorDataResult<Hall>("Zal tapılmadı.")
                : new SuccessDataResult<Hall>(hall);
        }

        // Restoran sahibinin öz sarayına yeni zal əlavə etməsi
        public async Task<IDataResult<Hall>> CreateAsync(CreateHallDto dto, int ownerId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == dto.VenueId && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorDataResult<Hall>("Bu saray sizə aid deyil.");

            var hall = new Hall
            {
                VenueId = dto.VenueId,
                Name = dto.Name,
                Capacity = dto.Capacity,
                Description = dto.Description,
                MinPrice = dto.MinPrice
            };

            await _hallDal.AddAsync(hall);
            return new SuccessDataResult<Hall>(hall, "Zal əlavə olundu.");
        }

        // Restoran sahibinin öz zalını redaktə etməsi
        public async Task<IResult> UpdateAsync(int id, UpdateHallDto dto, int ownerId)
        {
            var hall = await _hallDal.GetAsync(h => h.Id == id);
            if (hall is null)
                return new ErrorResult("Zal tapılmadı.");

            var venue = await _venueDal.GetAsync(v => v.Id == hall.VenueId && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorResult("Bu zal sizə aid deyil.");

            hall.Name = dto.Name;
            hall.Capacity = dto.Capacity;
            hall.Description = dto.Description;
            hall.MinPrice = dto.MinPrice;

            await _hallDal.UpdateAsync(hall);
            return new SuccessResult("Zal yeniləndi.");
        }

        // Restoran sahibinin öz zalını silməsi
        public async Task<IResult> DeleteAsync(int id, int ownerId)
        {
            var hall = await _hallDal.GetAsync(h => h.Id == id);
            if (hall is null)
                return new ErrorResult("Zal tapılmadı.");

            var venue = await _venueDal.GetAsync(v => v.Id == hall.VenueId && v.OwnerId == ownerId);
            if (venue is null)
                return new ErrorResult("Bu zal sizə aid deyil.");

            await _hallDal.DeleteAsync(hall);
            return new SuccessResult("Zal silindi.");
        }

        // Zalın verilmiş ay üçün (və ya bütün) dolu/boş günlərini qaytarır
        public async Task<IDataResult<List<HallAvailability>>> GetAvailabilityAsync(int hallId, int? year, int? month)
        {
            var query = await _availabilityDal.GetListAsync(a => a.HallId == hallId);
            var filtered = query.AsQueryable();

            if (year.HasValue && month.HasValue)
            {
                var first = new DateOnly(year.Value, month.Value, 1);
                var last = first.AddMonths(1);
                filtered = filtered.Where(a => a.Date >= first && a.Date < last);
            }

            var list = await filtered.ToListAsync();
            return new SuccessDataResult<List<HallAvailability>>(list);
        }

        // Bir günü əl ilə dolu/boş/tutulu etmək — sahib və ya sarayın administratoru edə bilər
        public async Task<IResult> SetAvailabilityAsync(int hallId, SetAvailabilityDto dto, int ownerId)
        {
            var hall = await _hallDal.GetAsync(h => h.Id == hallId);
            if (hall is null)
                return new ErrorResult("Zal tapılmadı.");

            if (!await _access.CanManageAsync(hall.VenueId, ownerId))
                return new ErrorResult("Bu zal sizə aid deyil.");

            var day = await _availabilityDal.GetAsync(a => a.HallId == hallId && a.Date == dto.Date);

            if (day is null)
            {
                await _availabilityDal.AddAsync(new HallAvailability
                {
                    HallId = hallId,
                    Date = dto.Date,
                    Status = dto.Status
                });
            }
            else
            {
                day.Status = dto.Status;
                await _availabilityDal.UpdateAsync(day);
            }

            return new SuccessResult("Təqvim yeniləndi.");
        }
    }
}
