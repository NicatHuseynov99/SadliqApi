using Business.Abstract;
using Entities.Dtos.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly IVenueService _venueService;

        public VenuesController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        // Bütün təsdiqlənmiş sadlıq saraylarının siyahısı
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _venueService.GetAllAsync();
            return Ok(result);
        }

        // Ana səhifənin siyahısı: axtarış, filtr, sıralama və səhifələmə
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] VenueSearchDto dto)
        {
            var result = await _venueService.SearchAsync(dto);
            return Ok(result);
        }

        // Filtr panelinin seçimləri: şəhərlər, özəlliklər, qiymət və tutum aralığı
        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            var result = await _venueService.GetFilterOptionsAsync();
            return Ok(result);
        }

        // Tək sarayın bütün detalları (şəkillər, zallar, menyular)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _venueService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // İdarə edilən saraylar: sahib öz saraylarını, administrator təyin olunduqlarını görür
        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueService.GetMineAsync(ownerId);
            return Ok(result);
        }

        // Yeni sadlıq sarayı əlavə edir (yalnız Owner)
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVenueDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueService.CreateAsync(dto, ownerId);
            return Ok(result);
        }

        // Owner öz sarayının məlumatlarını redaktə edir
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateVenueDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueService.UpdateAsync(id, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Owner öz sarayını silir
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueService.DeleteAsync(id, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Admin: təsdiq gözləyən sarayların siyahısı
        [Authorize(Roles = "SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> GetPending()
        {
            var result = await _venueService.GetPendingAsync();
            return Ok(result);
        }

        // Admin: sarayı təsdiqləyir, bundan sonra ictimai siyahıda görünür
        [Authorize(Roles = "SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _venueService.ApproveAsync(id);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
