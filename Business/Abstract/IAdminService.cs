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
        Task<IDataResult<List<AdminUserDto>>> GetUsersAsync(string? role, UserRole viewerRole);
        Task<IResult> ChangeRoleAsync(int userId, ChangeRoleDto dto, int currentAdminId, UserRole viewerRole);
    }
}
