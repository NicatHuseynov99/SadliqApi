using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Hall;

namespace Business.Abstract
{
    public interface IHallService
    {
        Task<IDataResult<List<Hall>>> GetByVenueIdAsync(int venueId);
        Task<IDataResult<Hall>> CreateAsync(CreateHallDto dto);
    }
}
