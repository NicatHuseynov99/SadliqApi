using Business.Abstract;
using Entities.Dtos.Hall;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HallsController : ControllerBase
    {
        private readonly IHallService _hallService;

        public HallsController(IHallService hallService)
        {
            _hallService = hallService;
        }

        // Bir sarayın bütün zallarını (tutum, qiymət, media, dolu günlər) qaytarır
        [HttpGet("by-venue/{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var result = await _hallService.GetByVenueIdAsync(venueId);
            return Ok(result);
        }

        // Restoran sahibinin öz sarayına yeni zal əlavə etməsi
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHallDto dto)
        {
            var result = await _hallService.CreateAsync(dto);
            return Ok(result);
        }
    }
}
