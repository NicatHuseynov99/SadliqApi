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
        private readonly IVenueFeatureDal _featureDal;

        public PlatformSettingsManager(
            IPlatformSettingDal settingDal,
            IVenueDal venueDal,
            IVenueFeatureDal featureDal)
        {
            _settingDal = settingDal;
            _venueDal = venueDal;
            _featureDal = featureDal;
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

        // Şəhərlər saray cədvəlindəki mətndən yığılır — ayrıca kitabça cədvəli yoxdur.
        // Ona görə «yenidən adlandırma» əslində bütün sarayların şəhərini dəyişməkdir.
        public async Task<IDataResult<List<ReferenceItemDto>>> GetCitiesAsync()
        {
            var query = await _venueDal.GetListAsync();

            var cities = await query
                .GroupBy(v => v.City)
                .Select(g => new ReferenceItemDto { Name = g.Key, VenueCount = g.Count() })
                .OrderByDescending(c => c.VenueCount)
                .ToListAsync();

            return new SuccessDataResult<List<ReferenceItemDto>>(cities);
        }

        public async Task<IResult> RenameCityAsync(RenameReferenceDto dto)
        {
            var oldName = dto.OldName?.Trim() ?? string.Empty;
            var newName = dto.NewName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return new ErrorResult("Köhnə və yeni ad boş ola bilməz.");

            if (oldName == newName)
                return new ErrorResult("Ad dəyişmir.");

            var query = await _venueDal.GetListAsync(v => v.City == oldName);
            var affected = await query.ExecuteUpdateAsync(s => s.SetProperty(v => v.City, newName));

            if (affected == 0)
                return new ErrorResult("Bu şəhərdə saray tapılmadı.");

            // Yeni ad artıq mövcuddursa iki siyahı birləşir — səhv yazılışları
            // düzəltməyin ən sadə yolu budur
            return new SuccessResult($"{affected} sarayın şəhəri «{newName}» olaraq dəyişdirildi.");
        }

        public async Task<IDataResult<List<ReferenceItemDto>>> GetFeaturesAsync()
        {
            var query = await _featureDal.GetListAsync();

            var features = await query
                .GroupBy(f => f.Name)
                .Select(g => new ReferenceItemDto
                {
                    Name = g.Key,
                    VenueCount = g.Select(f => f.VenueId).Distinct().Count()
                })
                .OrderByDescending(f => f.VenueCount)
                .ToListAsync();

            return new SuccessDataResult<List<ReferenceItemDto>>(features);
        }

        public async Task<IResult> RenameFeatureAsync(RenameReferenceDto dto)
        {
            var oldName = dto.OldName?.Trim() ?? string.Empty;
            var newName = dto.NewName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return new ErrorResult("Köhnə və yeni ad boş ola bilməz.");

            if (oldName == newName)
                return new ErrorResult("Ad dəyişmir.");

            var query = await _featureDal.GetListAsync(f => f.Name == oldName);
            var affected = await query.ExecuteUpdateAsync(s => s.SetProperty(f => f.Name, newName));

            if (affected == 0)
                return new ErrorResult("Bu özəllik heç bir sarayda yoxdur.");

            // Birləşdirmədən sonra eyni sarayda ad təkrarlana bilər — təkrarları atırıq
            var removed = await RemoveDuplicateFeaturesAsync(newName);

            var message = $"{affected} qeyd «{newName}» olaraq dəyişdirildi.";
            if (removed > 0) message += $" {removed} təkrar silindi.";

            return new SuccessResult(message);
        }

        public async Task<IResult> DeleteFeatureAsync(string name)
        {
            var trimmed = name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed))
                return new ErrorResult("Ad boş ola bilməz.");

            var query = await _featureDal.GetListAsync(f => f.Name == trimmed);
            var affected = await query.ExecuteDeleteAsync();

            if (affected == 0)
                return new ErrorResult("Bu özəllik heç bir sarayda yoxdur.");

            return new SuccessResult($"«{trimmed}» {affected} saraydan silindi.");
        }

        // Eyni sarayda eyni adlı özəllikdən yalnız birini saxlayır
        private async Task<int> RemoveDuplicateFeaturesAsync(string name)
        {
            var query = await _featureDal.GetListAsync(f => f.Name == name);
            var rows = await query.ToListAsync();

            var duplicates = rows
                .GroupBy(f => f.VenueId)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g.OrderBy(f => f.Id).Skip(1))
                .ToList();

            foreach (var duplicate in duplicates)
                await _featureDal.DeleteAsync(duplicate);

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
