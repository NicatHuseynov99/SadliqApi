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

        // Tək sarayın bütün detalları (şəkillər, zallar, menyular)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _venueService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // Yeni sadlıq sarayı əlavə edir (giriş tələb olunur)
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVenueDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venueService.CreateAsync(dto, ownerId);
            return Ok(result);
        }
    }
}
