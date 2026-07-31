using Core.Utilities.Results;
using Entities.Dtos.Common;
using Entities.Dtos.Venue;

namespace Business.Abstract
{
    // Platforma üzrə saray idarəetməsi — SuperAdmin və Support üçün.
    // Sarayın öz məlumatlarını redaktə etmək VenueService-də qalır: admin onsuz da
    // bütün saraylara sahib səlahiyyəti ilə çıxır (VenueAccessManager).
    public interface IAdminVenueService
    {
        Task<IDataResult<PagedResult<AdminVenueDto>>> SearchAsync(AdminVenueSearchDto dto);

        // Təsdiqi vermək və ya geri almaq — geri alınan saray saytda görünmür
        Task<IResult> SetApprovedAsync(int venueId, SetVenueApprovedDto dto);

        // Sarayı başqa sahibə keçirmək
        Task<IResult> ChangeOwnerAsync(int venueId, ChangeVenueOwnerDto dto);

        // Silinmiş sarayı zalları və menyuları ilə birlikdə geri qaytarmaq
        Task<IResult> RestoreAsync(int venueId);
    }
}
