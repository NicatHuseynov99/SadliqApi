using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Sahibkarın öz sarayları üzrə hesabatı
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
    public class OwnerStatsController : ControllerBase
    {
        private readonly IOwnerStatsService _service;

        public OwnerStatsController(IOwnerStatsService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int days = 30, [FromQuery] int? venueId = null)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.GetAsync(ownerId, days, venueId);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
