using Business.Abstract;
using Entities.Dtos.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Sahibkarın saray müraciəti. Giriş tələb olunmur: yeni sahibkar eyni anda
    // həm hesab açır, həm sarayını göndərir. Daxil olmuş istifadəçi üçün isə
    // mövcud hesab işlədilir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VenueApplicationsController : ControllerBase
    {
        private readonly IVenueApplicationService _service;

        public VenueApplicationsController(IVenueApplicationService service)
        {
            _service = service;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Submit([FromBody] VenueApplicationDto dto)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : (int?)null;

            var result = await _service.SubmitAsync(dto, userId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
