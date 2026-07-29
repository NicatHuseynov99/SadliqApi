using Business.Abstract;
using DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueAccessManager : IVenueAccessService
    {
        private readonly IVenueDal _venueDal;
        private readonly IVenueStaffDal _staffDal;

        public VenueAccessManager(IVenueDal venueDal, IVenueStaffDal staffDal)
        {
            _venueDal = venueDal;
            _staffDal = staffDal;
        }

        public async Task<bool> IsOwnerAsync(int venueId, int userId)
        {
            var venue = await _venueDal.GetAsync(v => v.Id == venueId && v.OwnerId == userId);
            return venue is not null;
        }

        // Sahib hər şeyi edə bilir; administrator yalnız təyin olunduğu sarayda
        public async Task<bool> CanManageAsync(int venueId, int userId)
        {
            if (await IsOwnerAsync(venueId, userId))
                return true;

            var staff = await _staffDal.GetAsync(
                s => s.VenueId == venueId && s.UserId == userId && s.IsActive);

            return staff is not null;
        }

        public async Task<List<int>> GetManagedVenueIdsAsync(int userId)
        {
            var ownedQuery = await _venueDal.GetListAsync(v => v.OwnerId == userId);
            var ownedIds = await ownedQuery.Select(v => v.Id).ToListAsync();

            var staffQuery = await _staffDal.GetListAsync(s => s.UserId == userId && s.IsActive);
            var assignedIds = await staffQuery.Select(s => s.VenueId).ToListAsync();

            return ownedIds.Union(assignedIds).ToList();
        }
    }
}
