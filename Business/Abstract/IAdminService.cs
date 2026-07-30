using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Admin;

namespace Business.Abstract
{
    // Platformanın idarəetməsi. Dəstək admini eyni əməliyyatları görür,
    // sadəcə SuperAdmin-lə bağlı heç nəyi görmür və dəyişə bilmir —
    // buna görə hər metod baxan istifadəçinin rolunu bilir.
    public interface IAdminService
    {
        Task<IDataResult<PlatformStatsDto>> GetStatsAsync(UserRole viewerRole);
        Task<IDataResult<List<AdminUserDto>>> GetUsersAsync(string? role, string? q, string? status, UserRole viewerRole);
        Task<IDataResult<AdminUserDetailDto>> GetUserDetailAsync(int userId, UserRole viewerRole);
        Task<IResult> ChangeRoleAsync(int userId, ChangeRoleDto dto, int currentAdminId, UserRole viewerRole);
        Task<IResult> SetUserActiveAsync(int userId, SetUserActiveDto dto, int currentAdminId, UserRole viewerRole);
        Task<IResult> ResetUserPasswordAsync(int userId, AdminResetPasswordDto dto, UserRole viewerRole);
    }
}
