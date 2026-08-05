using Business.Abstract;
using Entities.Dtos.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Platforma parametrləri və məlumat kitabçaları.
    // Dəstək admini burada platforma sahibi ilə eyni səlahiyyətdədir — parametrlərin
    // SuperAdmin hesabları ilə əlaqəsi yoxdur.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class PlatformController : ControllerBase
    {
        private readonly IPlatformSettingsService _service;

        public PlatformController(IPlatformSettingsService service)
        {
            _service = service;
        }

        // Kitabçaların silinmiş sətirləri yalnız admin və dəstək üçündür
        private bool IsStaff => User.IsInRole("SuperAdmin") || User.IsInRole("Support");

        [HttpGet]
        public async Task<IActionResult> GetSettings()
        {
            var result = await _service.GetSettingsAsync();
            return Ok(result);
        }

        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.UpdateSettingAsync(key, dto, adminId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }


        // ---------- İnterfeys mətnləri ----------

        [HttpGet]
        public async Task<IActionResult> GetTextCategories()
        {
            var result = await _service.GetTextCategoriesAsync();
            return Ok(result);
        }

        [HttpGet("{category}")]
        public async Task<IActionResult> GetTexts(string category, [FromQuery] string? q)
        {
            var result = await _service.GetTextsAsync(category, q);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateText([FromBody] AppTextFormDto dto)
        {
            var result = await _service.CreateTextAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateText(int id, [FromBody] AppTextFormDto dto)
        {
            var result = await _service.UpdateTextAsync(id, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteText(int id)
        {
            var result = await _service.DeleteTextAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // ---------- Şəhər kitabçası ----------

        // Şəhər siyahısı hər kəsə açıqdır: ana səhifədəki süzgəc,
        // sorğu forması və qeydiyyat anketi onu oxuyur
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetCities([FromQuery] bool includeDeleted = false)
        {
            // Silinmişləri yalnız platforma işçisi görə bilər
            var result = await _service.GetCitiesAsync(includeDeleted && IsStaff);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCity([FromBody] ReferenceFormDto dto)
        {
            var result = await _service.CreateCityAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] ReferenceFormDto dto)
        {
            var result = await _service.UpdateCityAsync(id, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var result = await _service.DeleteCityAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> RestoreCity(int id)
        {
            var result = await _service.RestoreCityAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // ---------- Özəllik kitabçası ----------

        // Özəlliklər də açıqdır — süzgəcdə və saray anketində göstərilir
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetFeatures([FromQuery] bool includeDeleted = false)
        {
            var result = await _service.GetFeaturesAsync(includeDeleted && IsStaff);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeature([FromBody] ReferenceFormDto dto)
        {
            var result = await _service.CreateFeatureAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeature(int id, [FromBody] ReferenceFormDto dto)
        {
            var result = await _service.UpdateFeatureAsync(id, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeature(int id)
        {
            var result = await _service.DeleteFeatureAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> RestoreFeature(int id)
        {
            var result = await _service.RestoreFeatureAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
