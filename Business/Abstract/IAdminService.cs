using Core.Utilities.Results;
using Entities.Dtos.Admin;

namespace Business.Abstract
{
    // Platformanın sahibi (SuperAdmin) üçün əməliyyatlar
    public interface IAdminService
    {
        Task<IDataResult<PlatformStatsDto>> GetStatsAsync();
        Task<IDataResult<List<AdminUserDto>>> GetUsersAsync(string? role);
        Task<IResult> ChangeRoleAsync(int userId, ChangeRoleDto dto, int currentAdminId);
    }
}
