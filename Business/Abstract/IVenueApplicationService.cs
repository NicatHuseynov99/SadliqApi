using Core.Utilities.Results;
using Entities.Dtos.Venue;

namespace Business.Abstract
{
    // Sahibkarın saray müraciəti. Saray təsdiqlənməmiş yaradılır —
    // admin və ya dəstək yoxlayıb təsdiqləyənə qədər saytda görünmür.
    public interface IVenueApplicationService
    {
        Task<IDataResult<VenueApplicationResultDto>> SubmitAsync(VenueApplicationDto dto, int? currentUserId);
    }
}
