using Business.Abstract;
using DataAccess.Abstract;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class VenueAccessManager : IVenueAccessService
    {
        private readonly IVenueDal _venueDal;
        private readonly IVenueStaffDal _staffDal;
        private readonly IUserDal _userDal;

        public VenueAccessManager(IVenueDal venueDal, IVenueStaffDal staffDal, IUserDal userDal)
        {
            _venueDal = venueDal;
            _staffDal = staffDal;
            _userDal = userDal;
        }

        // Platformanın sahibi və dəstək admini bütün saraylara sahib kimi baxır
        private async Task<bool> HasPlatformWideAccessAsync(int userId)
        {
            var user = await _userDal.GetAsync(u => u.Id == userId);
            return user?.Role is UserRole.SuperAdmin or UserRole.Support;
        }

        public async Task<bool> IsOwnerAsync(int venueId, int userId)
        {
            if (await HasPlatformWideAccessAsync(userId))
                return true;

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
            // SuperAdmin və dəstək admini bütün sarayları görür
            if (await HasPlatformWideAccessAsync(userId))
            {
                var allQuery = await _venueDal.GetListAsync();
                return await allQuery.Select(v => v.Id).ToListAsync();
            }

            var ownedQuery = await _venueDal.GetListAsync(v => v.OwnerId == userId);
            var ownedIds = await ownedQuery.Select(v => v.Id).ToListAsync();

            var staffQuery = await _staffDal.GetListAsync(s => s.UserId == userId && s.IsActive);
            var assignedIds = await staffQuery.Select(s => s.VenueId).ToListAsync();

            return ownedIds.Union(assignedIds).ToList();
        }
    }
}
