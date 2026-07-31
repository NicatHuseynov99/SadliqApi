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

        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var result = await _service.GetCitiesAsync();
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> RenameCity([FromBody] RenameReferenceDto dto)
        {
            var result = await _service.RenameCityAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFeatures()
        {
            var result = await _service.GetFeaturesAsync();
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> RenameFeature([FromBody] RenameReferenceDto dto)
        {
            var result = await _service.RenameFeatureAsync(dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteFeature(string name)
        {
            var result = await _service.DeleteFeatureAsync(name);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
