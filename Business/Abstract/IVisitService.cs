using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Visits;

namespace Business.Abstract
{
    // Ziyarətçi statistikası. Fəaliyyət jurnalından ayrıdır və rol
    // məhdudiyyəti yoxdur — burada SuperAdmin-ə aid gizli məlumat saxlanmır.
    public interface IVisitService
    {
        Task<IResult> TrackAsync(TrackPageDto dto, int? userId, string? ipAddress, string? userAgent);
        Task<IDataResult<VisitStatsDto>> GetStatsAsync(int days = 14);
        Task<IDataResult<PagedResult<VisitRowDto>>> SearchAsync(VisitSearchDto dto);
    }
}
