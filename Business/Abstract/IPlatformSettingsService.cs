using Core.Utilities.Results;
using Entities.Dtos.Platform;

namespace Business.Abstract
{
    // Platforma parametrləri və məlumat kitabçaları (şəhərlər, özəlliklər)
    public interface IPlatformSettingsService
    {
        Task<IDataResult<List<SettingDto>>> GetSettingsAsync();
        Task<IResult> UpdateSettingAsync(string key, UpdateSettingDto dto, int adminUserId);

        // Tək dəyər lazım olan yerlər üçün (məsələn ziyarət təmizləmə xidməti)
        Task<int> GetIntAsync(string key, int fallback);

        Task<IDataResult<List<ReferenceItemDto>>> GetCitiesAsync();
        Task<IResult> RenameCityAsync(RenameReferenceDto dto);

        Task<IDataResult<List<ReferenceItemDto>>> GetFeaturesAsync();
        Task<IResult> RenameFeatureAsync(RenameReferenceDto dto);
        Task<IResult> DeleteFeatureAsync(string name);
    }
}
