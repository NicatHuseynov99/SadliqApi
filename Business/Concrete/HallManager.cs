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

        public HallManager(IHallDal hallDal)
        {
            _hallDal = hallDal;
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

        // Restoran sahibinin sarayına yeni zal əlavə etməsi
        public async Task<IDataResult<Hall>> CreateAsync(CreateHallDto dto)
        {
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
    }
}
