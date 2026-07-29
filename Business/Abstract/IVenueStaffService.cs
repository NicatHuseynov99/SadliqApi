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

        // Administratoru deaktiv/aktiv edir
        Task<IResult> SetActiveAsync(int staffId, bool isActive, int ownerId);

        // Administratorun parolunu sıfırlayır
        Task<IResult> ResetPasswordAsync(int staffId, ResetStaffPasswordDto dto, int ownerId);
    }
}
