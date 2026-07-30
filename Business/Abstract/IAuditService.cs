using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Audit;
using Entities.Dtos.Common;

namespace Business.Abstract
{
    public interface IAuditService
    {
        // viewerRole SuperAdmin deyilsə, SuperAdmin-in əməliyyatları siyahıdan çıxarılır
        Task<IDataResult<PagedResult<AuditLog>>> SearchAsync(AuditSearchDto dto, UserRole viewerRole);
        Task<IDataResult<AuditFilterOptionsDto>> GetFilterOptionsAsync(UserRole viewerRole);
    }
}
