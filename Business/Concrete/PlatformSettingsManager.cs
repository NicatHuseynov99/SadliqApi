using Business.Abstract;
using Core.Utilities.Results;
using DataAccess.Abstract;
using Entities.Concrete;
using Entities.Dtos.Platform;
using Microsoft.EntityFrameworkCore;

namespace Business.Concrete
{
    public class PlatformSettingsManager : IPlatformSettingsService
    {
        private readonly IPlatformSettingDal _settingDal;
        private readonly IVenueDal _venueDal;

        // Sarayın öz özəllik sətirləri
        private readonly IVenueFeatureDal _venueFeatureDal;

        // Kitabçalar
        private readonly ICityDal _cityDal;
        private readonly IFeatureDal _featureCatalogDal;

        // İnterfeys mətnləri və onların yaddaşdakı nüsxəsi
        private readonly IAppTextDal _appTextDal;
        private readonly ITextCatalog _texts;

        public PlatformSettingsManager(
            IPlatformSettingDal settingDal,
            IVenueDal venueDal,
            IVenueFeatureDal venueFeatureDal,
            ICityDal cityDal,
            IFeatureDal featureCatalogDal,
            IAppTextDal appTextDal,
            ITextCatalog texts)
        {
            _settingDal = settingDal;
            _venueDal = venueDal;
            _venueFeatureDal = venueFeatureDal;
            _cityDal = cityDal;
            _featureCatalogDal = featureCatalogDal;
            _appTextDal = appTextDal;
            _texts = texts;
        }

        // Tanınan parametrlər. Bazada sətir yoxdursa buradakı ilkin dəyər işləyir —
        // yəni yeni parametr əlavə etmək üçün miqrasiya lazım deyil.
        public static readonly SettingDefinition[] Known =
        {
            new("visit_retention_days", "90", "number",
                "Ziyarət qeydlərinin saxlanma müddəti (gün)",
                "Bu müddətdən köhnə səhifə baxışları hər gecə avtomatik silinir."),

            new("feature_filter_min_count", "5", "number",
                "Filtrdə görünmək üçün minimum saray sayı",
                "Özəllik ən azı bu qədər sarayda varsa axtarış filtrində göstərilir."),

            new("stale_pending_days", "3", "number",
                "Cavabsız rezerv həddi (gün)",
                "Bu qədər gündür cavablanmayan sorğular paneldə «gecikən» sayılır."),

            new("review_auto_hide_reports", "0", "number",
                "Avtomatik gizlətmə üçün şikayət sayı",
                "Rəy bu qədər şikayət alanda avtomatik gizlədilir. 0 — söndürülüb.")
        };

        public async Task<IDataResult<List<SettingDto>>> GetSettingsAsync()
        {
            var saved = await (await _settingDal.GetListAsync()).ToDictionaryAsync(s => s.Key, s => s);

            var items = Known.Select(def =>
            {
                saved.TryGetValue(def.Key, out var row);
                return new SettingDto
                {
                    Key = def.Key,
                    Value = row?.Value ?? def.Default,
                    Label = def.Label,
                    Description = def.Description,
                    Type = def.Type,
                    UpdatedAt = row?.UpdatedAt
                };
            }).ToList();

            return new SuccessDataResult<List<SettingDto>>(items);
        }

        public async Task<IResult> UpdateSettingAsync(string key, UpdateSettingDto dto, int adminUserId)
        {
            var def = Known.FirstOrDefault(d => d.Key == key);
            if (def is null)
                return new ErrorResult("Belə parametr yoxdur.");

            var value = dto.Value?.Trim() ?? string.Empty;

            if (def.Type == "number" && (!int.TryParse(value, out var number) || number < 0))
                return new ErrorResult("Dəyər mənfi olmayan tam ədəd olmalıdır.");

            var existing = await _settingDal.GetAsync(s => s.Key == key);
            if (existing is null)
            {
                await _settingDal.AddAsync(new PlatformSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedByUserId = adminUserId
                });
            }
            else
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedByUserId = adminUserId;
                await _settingDal.UpdateAsync(existing);
            }

            return new SuccessResult("Parametr yeniləndi.");
        }

        public async Task<int> GetIntAsync(string key, int fallback)
        {
            var row = await _settingDal.GetAsync(s => s.Key == key);
            if (row is not null && int.TryParse(row.Value, out var value))
                return value;

            var def = Known.FirstOrDefault(d => d.Key == key);
            if (def is not null && int.TryParse(def.Default, out var fromDefault))
                return fromDefault;

            return fallback;
        }

        // ---------- İnterfeys mətnləri ----------
        // Kateqoriyaların izahı kodda qalır, çünki bu, kodun hansı açarı hardan
        // oxuduğunu göstərir — mətnin özü isə bazadadır.
        private static readonly (string Name, string Label, string Description, bool UsesExtra)[] Categories =
        {
            (TextCategories.AuditAction, "Əməliyyat adları",
                "Fəaliyyət jurnalında və süzgəcdə görünən adlar.", false),

            (TextCategories.AuditEndpoint, "Jurnal izahları",
                "Hansı sorğunun jurnalda necə yazılacağı. Əlavə sahə — əməliyyat kodu.", true),

            (TextCategories.TableLabel, "Cədvəl adları",
                "Baza bölməsində cədvəllərin oxunaqlı adları.", false),

            (TextCategories.PageType, "Səhifə növləri",
                "Ziyarətçi hesabatında səhifələrin adları.", false),

            (TextCategories.ReportReason, "Şikayət səbəbləri",
                "İstifadəçinin rəyi şikayət edərkən seçdiyi səbəblər.", false),

            (TextCategories.HideReason, "Gizlətmə səbəbləri",
                "Moderatorun rəyi gizlədərkən seçdiyi hazır səbəblər.", false)
        };

        public async Task<IDataResult<List<TextCategoryDto>>> GetTextCategoriesAsync()
        {
            var counts = await (await _appTextDal.GetListAsync())
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var items = Categories.Select(c => new TextCategoryDto
            {
                Name = c.Name,
                Label = c.Label,
                Description = c.Description,
                UsesExtra = c.UsesExtra,
                Count = counts.FirstOrDefault(x => x.Category == c.Name)?.Count ?? 0
            }).ToList();

            return new SuccessDataResult<List<TextCategoryDto>>(items);
        }

        public async Task<IDataResult<List<AppTextDto>>> GetTextsAsync(string category, string? q)
        {
            var query = await _appTextDal.GetListAsync(t => t.Category == category);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(t => t.Key.ToLower().Contains(term) || t.Value.ToLower().Contains(term));
            }

            var items = await query
                .OrderBy(t => t.Key)
                .Select(t => new AppTextDto
                {
                    Id = t.Id,
                    Category = t.Category,
                    Key = t.Key,
                    Value = t.Value,
                    Extra = t.Extra,
                    UpdatedAt = t.UpdatedAt
                })
                .ToListAsync();

            return new SuccessDataResult<List<AppTextDto>>(items);
        }

        public async Task<IResult> CreateTextAsync(AppTextFormDto dto)
        {
            var validation = ValidateText(dto);
            if (validation is not null)
                return new ErrorResult(validation);

            var key = dto.Key.Trim();

            var existing = await _appTextDal.GetAsync(t => t.Category == dto.Category && t.Key == key);
            if (existing is not null)
                return new ErrorResult("Bu açar həmin kateqoriyada artıq var.");

            await _appTextDal.AddAsync(new AppText
            {
                Category = dto.Category.Trim(),
                Key = key,
                Value = dto.Value.Trim(),
                Extra = string.IsNullOrWhiteSpace(dto.Extra) ? null : dto.Extra.Trim(),
                UpdatedAt = DateTime.UtcNow
            });

            _texts.Invalidate();
            return new SuccessResult("Mətn əlavə olundu.");
        }

        public async Task<IResult> UpdateTextAsync(int id, AppTextFormDto dto)
        {
            var text = await _appTextDal.GetAsync(t => t.Id == id);
            if (text is null)
                return new ErrorResult("Mətn tapılmadı.");

            var validation = ValidateText(dto);
            if (validation is not null)
                return new ErrorResult(validation);

            var key = dto.Key.Trim();

            var duplicate = await _appTextDal.GetAsync(
                t => t.Category == text.Category && t.Key == key && t.Id != id);

            if (duplicate is not null)
                return new ErrorResult("Bu açar həmin kateqoriyada artıq var.");

            // Kateqoriya dəyişmir — açar kodla bağlıdır
            text.Key = key;
            text.Value = dto.Value.Trim();
            text.Extra = string.IsNullOrWhiteSpace(dto.Extra) ? null : dto.Extra.Trim();
            text.UpdatedAt = DateTime.UtcNow;

            await _appTextDal.UpdateAsync(text);

            _texts.Invalidate();
            return new SuccessResult("Mətn yeniləndi.");
        }

        public async Task<IResult> DeleteTextAsync(int id)
        {
            var text = await _appTextDal.GetAsync(t => t.Id == id);
            if (text is null)
                return new ErrorResult("Mətn tapılmadı.");

            await _appTextDal.DeleteAsync(text);

            _texts.Invalidate();

            // Mətn silinsə kod açarın özünü göstərəcək — bu, xəbərdarlıqdır
            return new SuccessResult($"«{text.Key}» silindi. Bu açar artıq tərcüməsiz göstəriləcək.");
        }

        private static string? ValidateText(AppTextFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Category) || Categories.All(c => c.Name != dto.Category))
                return "Kateqoriya düzgün seçilməyib.";

            if (string.IsNullOrWhiteSpace(dto.Key))
                return "Açar boş ola bilməz.";

            if (string.IsNullOrWhiteSpace(dto.Value))
                return "Mətn boş ola bilməz.";

            return null;
        }

        // ---------- Şəhər kitabçası ----------
        // Şəhərlər artıq ayrıca cədvəldədir: boş şəhər də yaradıla bilər, ad dəyişəndə
        // həmin şəhərdəki sarayların mətn sahəsi də sinxron yenilənir.

        public async Task<IDataResult<List<ReferenceItemDto>>> GetCitiesAsync(bool includeDeleted = false)
        {
            var query = (await _cityDal.GetListAsync()).IgnoreQueryFilters();
            if (!includeDeleted) query = query.Where(c => !c.IsDeleted);

            var cities = await query.ToListAsync();

            // İşlənmə sayı sarayların öz mətn sahəsindən hesablanır
            var counts = await (await _venueDal.GetListAsync())
                .GroupBy(v => v.City)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            var items = cities
                .Select(c => new ReferenceItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    SortOrder = c.SortOrder,
                    IsDeleted = c.IsDeleted,
                    VenueCount = counts.FirstOrDefault(x => x.Name == c.Name)?.Count ?? 0
                })
                .OrderBy(c => c.SortOrder)
                .ThenByDescending(c => c.VenueCount)
                .ThenBy(c => c.Name)
                .ToList();

            return new SuccessDataResult<List<ReferenceItemDto>>(items);
        }

        public async Task<IResult> CreateCityAsync(ReferenceFormDto dto)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new ErrorResult("Şəhər adı boş ola bilməz.");

            var existing = await FindCityAsync(name);
            if (existing is not null)
            {
                // Silinmiş şəhər eyni adla yenidən əlavə edilirsə, sadəcə geri qaytarırıq
                if (!existing.IsDeleted)
                    return new ErrorResult("Bu şəhər siyahıda var.");

                existing.IsDeleted = false;
                existing.DeletedAt = null;
                await _cityDal.UpdateAsync(existing);
                return new SuccessResult($"«{name}» siyahıya geri qaytarıldı.");
            }

            await _cityDal.AddAsync(new City
            {
                Name = name,
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            });

            return new SuccessResult($"«{name}» əlavə olundu.");
        }

        public async Task<IResult> UpdateCityAsync(int id, ReferenceFormDto dto)
        {
            var city = await FindCityAsync(id);
            if (city is null)
                return new ErrorResult("Şəhər tapılmadı.");

            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new ErrorResult("Şəhər adı boş ola bilməz.");

            var duplicate = await FindCityAsync(name);
            if (duplicate is not null && duplicate.Id != id)
                return new ErrorResult("Bu adda şəhər artıq var.");

            var oldName = city.Name;
            city.Name = name;
            city.SortOrder = dto.SortOrder;
            await _cityDal.UpdateAsync(city);

            // Ad dəyişibsə saraylar da yenilənir, yoxsa siyahı ilə data uyuşmaz
            var affected = 0;
            if (oldName != name)
            {
                var venues = await _venueDal.GetListAsync(v => v.City == oldName);
                affected = await venues.ExecuteUpdateAsync(s => s.SetProperty(v => v.City, name));
            }

            return new SuccessResult(affected > 0
                ? $"Ad dəyişdirildi, {affected} saray yeniləndi."
                : "Şəhər yeniləndi.");
        }

        // İşlənən şəhəri silmirik — əvvəlcə sarayları başqa şəhərə keçirmək lazımdır
        public async Task<IResult> DeleteCityAsync(int id)
        {
            var city = await FindCityAsync(id);
            if (city is null)
                return new ErrorResult("Şəhər tapılmadı.");

            var venues = await _venueDal.GetListAsync(v => v.City == city.Name);
            var count = await venues.CountAsync();

            if (count > 0)
                return new ErrorResult($"Bu şəhərdə {count} saray var. Əvvəlcə onların şəhərini dəyişin.");

            await _cityDal.DeleteAsync(city);
            return new SuccessResult($"«{city.Name}» siyahıdan çıxarıldı.");
        }

        public async Task<IResult> RestoreCityAsync(int id)
        {
            var city = await FindCityAsync(id);
            if (city is null)
                return new ErrorResult("Şəhər tapılmadı.");

            if (!city.IsDeleted)
                return new ErrorResult("Bu şəhər silinməyib.");

            city.IsDeleted = false;
            city.DeletedAt = null;
            await _cityDal.UpdateAsync(city);

            return new SuccessResult($"«{city.Name}» geri qaytarıldı.");
        }

        // ---------- Özəllik kitabçası ----------

        public async Task<IDataResult<List<ReferenceItemDto>>> GetFeaturesAsync(bool includeDeleted = false)
        {
            var query = (await _featureCatalogDal.GetListAsync()).IgnoreQueryFilters();
            if (!includeDeleted) query = query.Where(f => !f.IsDeleted);

            var features = await query.ToListAsync();

            var counts = await (await _venueFeatureDal.GetListAsync())
                .GroupBy(f => f.Name)
                .Select(g => new { Name = g.Key, Count = g.Select(x => x.VenueId).Distinct().Count() })
                .ToListAsync();

            var items = features
                .Select(f => new ReferenceItemDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Icon = f.Icon,
                    SortOrder = f.SortOrder,
                    IsDeleted = f.IsDeleted,
                    VenueCount = counts.FirstOrDefault(x => x.Name == f.Name)?.Count ?? 0
                })
                .OrderBy(f => f.SortOrder)
                .ThenByDescending(f => f.VenueCount)
                .ThenBy(f => f.Name)
                .ToList();

            return new SuccessDataResult<List<ReferenceItemDto>>(items);
        }

        public async Task<IResult> CreateFeatureAsync(ReferenceFormDto dto)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new ErrorResult("Özəllik adı boş ola bilməz.");

            var existing = await FindFeatureAsync(name);
            if (existing is not null)
            {
                if (!existing.IsDeleted)
                    return new ErrorResult("Bu özəllik siyahıda var.");

                existing.IsDeleted = false;
                existing.DeletedAt = null;
                await _featureCatalogDal.UpdateAsync(existing);
                return new SuccessResult($"«{name}» siyahıya geri qaytarıldı.");
            }

            await _featureCatalogDal.AddAsync(new Feature
            {
                Name = name,
                Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim(),
                SortOrder = dto.SortOrder,
                CreatedAt = DateTime.UtcNow
            });

            return new SuccessResult($"«{name}» əlavə olundu.");
        }

        public async Task<IResult> UpdateFeatureAsync(int id, ReferenceFormDto dto)
        {
            var feature = await FindFeatureAsync(id);
            if (feature is null)
                return new ErrorResult("Özəllik tapılmadı.");

            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new ErrorResult("Özəllik adı boş ola bilməz.");

            var duplicate = await FindFeatureAsync(name);
            if (duplicate is not null && duplicate.Id != id)
                return new ErrorResult("Bu adda özəllik artıq var.");

            var oldName = feature.Name;
            feature.Name = name;
            feature.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? null : dto.Icon.Trim();
            feature.SortOrder = dto.SortOrder;
            await _featureCatalogDal.UpdateAsync(feature);

            var affected = 0;
            var removed = 0;

            if (oldName != name)
            {
                var rows = await _venueFeatureDal.GetListAsync(f => f.Name == oldName);
                affected = await rows.ExecuteUpdateAsync(s => s.SetProperty(f => f.Name, name));

                // Birləşdirmədən sonra eyni sarayda ad təkrarlana bilər
                removed = await RemoveDuplicateFeaturesAsync(name);
            }

            var message = affected > 0 ? $"Ad dəyişdirildi, {affected} qeyd yeniləndi." : "Özəllik yeniləndi.";
            if (removed > 0) message += $" {removed} təkrar silindi.";

            return new SuccessResult(message);
        }

        // Kitabçadan çıxarılan özəllik sarayların da özəllik siyahısından silinir
        public async Task<IResult> DeleteFeatureAsync(int id)
        {
            var feature = await FindFeatureAsync(id);
            if (feature is null)
                return new ErrorResult("Özəllik tapılmadı.");

            var rows = await _venueFeatureDal.GetListAsync(f => f.Name == feature.Name);
            var affected = await rows.ExecuteDeleteAsync();

            await _featureCatalogDal.DeleteAsync(feature);

            return new SuccessResult(affected > 0
                ? $"«{feature.Name}» siyahıdan və {affected} saraydan silindi."
                : $"«{feature.Name}» siyahıdan çıxarıldı.");
        }

        public async Task<IResult> RestoreFeatureAsync(int id)
        {
            var feature = await FindFeatureAsync(id);
            if (feature is null)
                return new ErrorResult("Özəllik tapılmadı.");

            if (!feature.IsDeleted)
                return new ErrorResult("Bu özəllik silinməyib.");

            feature.IsDeleted = false;
            feature.DeletedAt = null;
            await _featureCatalogDal.UpdateAsync(feature);

            // Sarayların özəllik sətirləri silmə zamanı getdiyi üçün geri qayıtmır —
            // özəllik yenidən siyahıdadır, saraylara isə əl ilə əlavə olunur
            return new SuccessResult($"«{feature.Name}» siyahıya geri qaytarıldı.");
        }

        // ---------- köməkçilər ----------
        // Silinmiş sətirlərlə də işləmək lazımdır, ona görə filtri söndürürük

        private async Task<City?> FindCityAsync(int id) =>
            await (await _cityDal.GetListAsync()).IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);

        private async Task<City?> FindCityAsync(string name) =>
            await (await _cityDal.GetListAsync()).IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

        private async Task<Feature?> FindFeatureAsync(int id) =>
            await (await _featureCatalogDal.GetListAsync()).IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);

        private async Task<Feature?> FindFeatureAsync(string name) =>
            await (await _featureCatalogDal.GetListAsync()).IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Name.ToLower() == name.ToLower());

        // Eyni sarayda eyni adlı özəllikdən yalnız birini saxlayır
        private async Task<int> RemoveDuplicateFeaturesAsync(string name)
        {
            var query = await _venueFeatureDal.GetListAsync(f => f.Name == name);
            var rows = await query.ToListAsync();

            var duplicates = rows
                .GroupBy(f => f.VenueId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(f => f.Id).Skip(1))
                .ToList();

            foreach (var duplicate in duplicates)
                await _venueFeatureDal.DeleteAsync(duplicate);

            return duplicates.Count;
        }
    }


    // Tanınan parametrin təsviri — bazada sətir olmasa da panel onu göstərir
    public record SettingDefinition(
        string Key,
        string Default,
        string Type,
        string Label,
        string Description);
}
