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
        private readonly IVenueStaffHallDal _staffHallDal;
        private readonly IHallDal _hallDal;

        public VenueAccessManager(
            IVenueDal venueDal,
            IVenueStaffDal staffDal,
            IUserDal userDal,
            IVenueStaffHallDal staffHallDal,
            IHallDal hallDal)
        {
            _venueDal = venueDal;
            _staffDal = staffDal;
            _userDal = userDal;
            _staffHallDal = staffHallDal;
            _hallDal = hallDal;
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

        // Təsdiqlənməmiş saray dondurulmuş vəziyyətdədir: sahibkar onu görür,
        // amma nə redaktə edə, nə zal/menyu əlavə edə bilir. Belə olmasa müraciət
        // yoxlanılarkən məlumat dəyişər və təsdiq mənasını itirər.
        public async Task<bool> CanModifyAsync(int venueId, int userId)
        {
            if (await HasPlatformWideAccessAsync(userId))
                return true;

            if (!await CanManageAsync(venueId, userId))
                return false;

            var venue = await _venueDal.GetAsync(v => v.Id == venueId);
            return venue?.IsApproved == true;
        }

        public async Task<bool> CanModifyHallAsync(int hallId, int userId)
        {
            var hall = await _hallDal.GetAsync(h => h.Id == hallId);
            if (hall is null)
                return false;

            if (!await CanModifyAsync(hall.VenueId, userId))
                return false;

            var allowedHalls = await GetAllowedHallIdsAsync(userId);
            return allowedHalls is null || allowedHalls.Contains(hallId);
        }

        // Administratorun konkret səlahiyyəti var? Sahib və platforma işçisi
        // üçün həmişə true — məhdudiyyət yalnız təyin olunmuş administratora aiddir.
        public async Task<bool> HasStaffPermissionAsync(int venueId, int userId, StaffPermission permission)
        {
            if (await IsOwnerAsync(venueId, userId))
                return true;

            var staff = await _staffDal.GetAsync(
                s => s.VenueId == venueId && s.UserId == userId && s.IsActive);

            if (staff is null)
                return false;

            return permission switch
            {
                StaffPermission.ConfirmReservations => staff.CanConfirmReservations,
                StaffPermission.EditCalendar => staff.CanEditCalendar,
                StaffPermission.SendOffers => staff.CanSendOffers,
                _ => false
            };
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

        // Administratorun icazəli zalları.
        // null = məhdudiyyət yoxdur (sahib, dəstək admini və ya zal seçilməmiş administrator)
        public async Task<List<int>?> GetAllowedHallIdsAsync(int userId)
        {
            if (await HasPlatformWideAccessAsync(userId))
                return null;

            var staffQuery = await _staffDal.GetListAsync(s => s.UserId == userId && s.IsActive);
            var staffList = await staffQuery.ToListAsync();

            // Heç bir təyinatı yoxdursa, deməli sahibdir — məhdudiyyət qoyulmur
            if (staffList.Count == 0)
                return null;

            var staffIds = staffList.Select(s => s.Id).ToList();
            var hallQuery = await _staffHallDal.GetListAsync(h => staffIds.Contains(h.VenueStaffId));
            var hallIds = await hallQuery.Select(h => h.HallId).ToListAsync();

            // Zal seçilməyibsə administrator sarayın bütün zallarına baxa bilir
            if (hallIds.Count == 0)
                return null;

            return hallIds;
        }

        // Konkret zalda iş görmək icazəsi
        public async Task<bool> CanManageHallAsync(int hallId, int userId)
        {
            var hall = await _hallDal.GetAsync(h => h.Id == hallId);
            if (hall is null)
                return false;

            if (!await CanManageAsync(hall.VenueId, userId))
                return false;

            var allowedHalls = await GetAllowedHallIdsAsync(userId);
            return allowedHalls is null || allowedHalls.Contains(hallId);
        }
    }
}
