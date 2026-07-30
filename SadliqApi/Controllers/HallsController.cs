using Business.Abstract;
using Entities.Dtos.Hall;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HallsController : ControllerBase
    {
        private readonly IHallService _hallService;

        public HallsController(IHallService hallService)
        {
            _hallService = hallService;
        }

        // Bir sarayın bütün zallarını (tutum, qiymət, media, dolu günlər) qaytarır
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var result = await _hallService.GetByVenueIdAsync(venueId);
            return Ok(result);
        }

        // Tək zalın detalları
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _hallService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // Restoran sahibinin öz sarayına yeni zal əlavə etməsi
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHallDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _hallService.CreateAsync(dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Restoran sahibinin öz zalını redaktə etməsi
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateHallDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _hallService.UpdateAsync(id, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Restoran sahibinin öz zalını silməsi
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _hallService.DeleteAsync(id, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Zalın verilmiş ay üçün (?year=2026&month=8) dolu/boş günləri
        [HttpGet("{hallId}")]
        public async Task<IActionResult> GetAvailability(int hallId, [FromQuery] int? year, [FromQuery] int? month)
        {
            var result = await _hallService.GetAvailabilityAsync(hallId, year, month);
            return Ok(result);
        }

        // Bir günü əl ilə dolu/boş/tutulu etmək — sahib və ya sarayın administratoru
        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpPut("{hallId}")]
        public async Task<IActionResult> SetAvailability(int hallId, [FromBody] SetAvailabilityDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _hallService.SetAvailabilityAsync(hallId, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
