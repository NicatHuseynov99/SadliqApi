using Core.Utilities.Results;
using Entities.Dtos.Staff;

namespace Business.Abstract
{
    public interface IVenueStaffService
    {
        // Sahibin bütün saraylarındakı administratorlar
        Task<IDataResult<List<VenueStaffDto>>> GetMineAsync(int ownerId);

        // Yeni administrator hesabı yaradır və sarayına təyin edir
        Task<IDataResult<VenueStaffDto>> CreateAsync(CreateVenueStaffDto dto, int ownerId);

        // Administratorun məlumatlarını yeniləyir
        Task<IResult> UpdateAsync(int staffId, UpdateVenueStaffDto dto, int ownerId);

        // Administratoru deaktiv/aktiv edir
        Task<IResult> SetActiveAsync(int staffId, bool isActive, int ownerId);

        // Təyinatı tamamilə silir (hesabın özü qalır)
        Task<IResult> DeleteAsync(int staffId, int ownerId);

        // Administratorun işləyə biləcəyi zalları təyin edir
        Task<IResult> SetHallsAsync(int staffId, SetStaffHallsDto dto, int ownerId);

        // Administratorun səlahiyyətləri: rezerv təsdiqi, təqvim, təkliflər
        Task<IResult> SetPermissionsAsync(int staffId, SetStaffPermissionsDto dto, int ownerId);

        // Administratorun parolunu sıfırlayır
        Task<IResult> ResetPasswordAsync(int staffId, ResetStaffPasswordDto dto, int ownerId);
    }
}
