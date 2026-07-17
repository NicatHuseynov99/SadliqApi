using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Venue;

namespace Business.Abstract
{
    public interface IVenueService
    {
        Task<IDataResult<List<Venue>>> GetAllAsync();
        Task<IDataResult<Venue>> GetByIdAsync(int id);
        Task<IDataResult<Venue>> CreateAsync(CreateVenueDto dto, int ownerId);
    }
}
