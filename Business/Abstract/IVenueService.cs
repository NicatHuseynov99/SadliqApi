using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Venue;

namespace Business.Abstract
{
    public interface IVenueService
    {
        Task<IDataResult<List<Venue>>> GetAllAsync();
        Task<IDataResult<List<Venue>>> SearchAsync(string? city, DateOnly? date, decimal? minPrice, decimal? maxPrice, int? capacity);
        Task<IDataResult<Venue>> GetByIdAsync(int id);
        Task<IDataResult<List<Venue>>> GetMineAsync(int ownerId);
        Task<IDataResult<Venue>> CreateAsync(CreateVenueDto dto, int ownerId);
        Task<IResult> UpdateAsync(int id, UpdateVenueDto dto, int ownerId);
        Task<IResult> DeleteAsync(int id, int ownerId);

        // Admin əməliyyatları
        Task<IDataResult<List<Venue>>> GetPendingAsync();
        Task<IResult> ApproveAsync(int id);
    }
}
