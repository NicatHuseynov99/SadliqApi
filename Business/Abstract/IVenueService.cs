using Core.Utilities.Results;
using Entities.Concrete;
using Entities.Dtos.Common;
using Entities.Dtos.Venue;

namespace Business.Abstract
{
    public interface IVenueService
    {
        Task<IDataResult<List<Venue>>> GetAllAsync();
        Task<IDataResult<PagedResult<Venue>>> SearchAsync(VenueSearchDto dto);
        Task<IDataResult<VenueFilterOptionsDto>> GetFilterOptionsAsync();

        // Xəritə rejimi — markerlər üçün yüngül siyahı
        Task<IDataResult<VenueMapResultDto>> MapSearchAsync(VenueMapSearchDto dto);

        // Axtarış qutusunun təklifləri
        Task<IDataResult<List<SearchSuggestionDto>>> SuggestAsync(string? q);
        Task<IDataResult<Venue>> GetByIdAsync(int id, int? viewerUserId = null);

        // Müştəriyə göstərilən etibar siqnalları: cavab müddəti, təsdiq faizi,
        // cavabsız qalan və təsdiqdən sonra ləğv edilən sorğular
        Task<IDataResult<VenueTrustDto>> GetTrustAsync(int venueId);
        Task<IDataResult<List<Venue>>> GetMineAsync(int ownerId);
        Task<IDataResult<Venue>> CreateAsync(CreateVenueDto dto, int ownerId);
        Task<IResult> UpdateAsync(int id, UpdateVenueDto dto, int ownerId);
        Task<IResult> DeleteAsync(int id, int ownerId);

        // Admin əməliyyatları
        Task<IDataResult<List<Venue>>> GetPendingAsync();
        Task<IResult> ApproveAsync(int id);
    }
}
