using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Menu;

namespace Business.Abstract
{
    public interface IMenuService
    {
        Task<IDataResult<List<Menu>>> GetByVenueIdAsync(int venueId);
        Task<IDataResult<Menu>> CreateAsync(CreateMenuDto dto);
    }
}
