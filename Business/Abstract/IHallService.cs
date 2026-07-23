using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Hall;

namespace Business.Abstract
{
    public interface IHallService
    {
        Task<IDataResult<List<Hall>>> GetByVenueIdAsync(int venueId);
        Task<IDataResult<Hall>> GetByIdAsync(int id);
        Task<IDataResult<Hall>> CreateAsync(CreateHallDto dto, int ownerId);
        Task<IResult> UpdateAsync(int id, UpdateHallDto dto, int ownerId);
        Task<IResult> DeleteAsync(int id, int ownerId);

        // Təqvim (dolu/boş günlər)
        Task<IDataResult<List<HallAvailability>>> GetAvailabilityAsync(int hallId, int? year, int? month);
        Task<IResult> SetAvailabilityAsync(int hallId, SetAvailabilityDto dto, int ownerId);
    }
}
