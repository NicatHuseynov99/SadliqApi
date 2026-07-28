using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class FavoriteManager : IFavoriteService
    {
        private readonly IFavoriteDal _favoriteDal;
        private readonly IVenueDal _venueDal;

        public FavoriteManager(IFavoriteDal favoriteDal, IVenueDal venueDal)
        {
            _favoriteDal = favoriteDal;
            _venueDal = venueDal;
        }

        // Müştərinin seçilmiş saraylarını kart üçün lazım olan detallarla qaytarır
        public async Task<IDataResult<List<Venue>>> GetMineAsync(int userId)
        {
            var favorites = await _favoriteDal.GetListAsync(f => f.UserId == userId);
            var venueIds = await favorites.Select(f => f.VenueId).ToListAsync();

            var query = await _venueDal.GetListAsync(v => venueIds.Contains(v.Id));
            var venues = await query
                .Include(v => v.Images)
                .Include(v => v.Halls)
                .Include(v => v.Menus)
                .Include(v => v.Features)
                .Include(v => v.Reviews)
                .ToListAsync();

            return new SuccessDataResult<List<Venue>>(venues);
        }

        // Yalnız Id-lər — ana səhifədə hansı ürəyin dolu olacağını bilmək üçün
        public async Task<IDataResult<List<int>>> GetMyVenueIdsAsync(int userId)
        {
            var favorites = await _favoriteDal.GetListAsync(f => f.UserId == userId);
            var ids = await favorites.Select(f => f.VenueId).ToListAsync();

            return new SuccessDataResult<List<int>>(ids);
        }

        // Ürəyə basanda: varsa silinir, yoxdursa əlavə olunur. Cavab: sonda seçilmişdədirmi
        public async Task<IDataResult<bool>> ToggleAsync(int venueId, int userId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId);
            if (venue is null)
                return new ErrorDataResult<bool>("Saray tapılmadı.");

            var existing = await _favoriteDal.GetAsync(f => f.UserId == userId && f.VenueId == venueId);

            if (existing is not null)
            {
                await _favoriteDal.DeleteAsync(existing);
                return new SuccessDataResult<bool>(false, "Seçilmişlərdən silindi.");
            }

            await _favoriteDal.AddAsync(new Favorite { UserId = userId, VenueId = venueId });
            return new SuccessDataResult<bool>(true, "Seçilmişlərə əlavə olundu.");
        }
    }
}
