using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Reservation;

namespace Business.Abstract
{
    // Platforma üzrə bütün rezervlərə baxış — SuperAdmin və Support üçün
    public interface IAdminReservationService
    {
        Task<IDataResult<AdminReservationStatsDto>> GetStatsAsync();
        Task<IDataResult<PagedResult<AdminReservationDto>>> SearchAsync(AdminReservationSearchDto dto);
    }
}
