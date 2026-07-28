using Core.Utilities.Results;
using Entities.Concrete;

namespace Business.Abstract
{
    public interface IFavoriteService
    {
        Task<IDataResult<List<Venue>>> GetMineAsync(int userId);
        Task<IDataResult<List<int>>> GetMyVenueIdsAsync(int userId);

        // Ürəyə basanda: seçilmişdə varsa silir, yoxdursa əlavə edir
        Task<IDataResult<bool>> ToggleAsync(int venueId, int userId);
    }
}
