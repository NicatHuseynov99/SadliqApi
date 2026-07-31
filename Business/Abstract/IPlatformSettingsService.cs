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

        // İnterfeys mətnləri — tam CRUD
        Task<IDataResult<List<TextCategoryDto>>> GetTextCategoriesAsync();
        Task<IDataResult<List<AppTextDto>>> GetTextsAsync(string category, string? q);
        Task<IResult> CreateTextAsync(AppTextFormDto dto);
        Task<IResult> UpdateTextAsync(int id, AppTextFormDto dto);
        Task<IResult> DeleteTextAsync(int id);

        // Şəhər kitabçası — tam CRUD
        Task<IDataResult<List<ReferenceItemDto>>> GetCitiesAsync(bool includeDeleted = false);
        Task<IResult> CreateCityAsync(ReferenceFormDto dto);
        Task<IResult> UpdateCityAsync(int id, ReferenceFormDto dto);
        Task<IResult> DeleteCityAsync(int id);
        Task<IResult> RestoreCityAsync(int id);

        // Özəllik kitabçası — tam CRUD
        Task<IDataResult<List<ReferenceItemDto>>> GetFeaturesAsync(bool includeDeleted = false);
        Task<IResult> CreateFeatureAsync(ReferenceFormDto dto);
        Task<IResult> UpdateFeatureAsync(int id, ReferenceFormDto dto);
        Task<IResult> DeleteFeatureAsync(int id);
        Task<IResult> RestoreFeatureAsync(int id);
    }
}
