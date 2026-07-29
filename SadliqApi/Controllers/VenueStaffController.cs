using Business.Abstract;
using Entities.Dtos.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Administrator hesablarını yalnız saray rəhbərliyi (Owner) idarə edir.
    // VenueAdmin özü başqa administrator yarada bilməz — səlahiyyət artırmanın qarşısı alınır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Owner")]
    public class VenueStaffController : ControllerBase
    {
        private readonly IVenueStaffService _staffService;

        public VenueStaffController(IVenueStaffService staffService)
        {
            _staffService = staffService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Sahibin bütün saraylarındakı administratorlar
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _staffService.GetMineAsync(CurrentUserId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVenueStaffDto dto)
        {
            var result = await _staffService.CreateAsync(dto, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Girişi bağlayır və ya yenidən açır
        [HttpPut("{id}")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool isActive)
        {
            var result = await _staffService.SetActiveAsync(id, isActive, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetStaffPasswordDto dto)
        {
            var result = await _staffService.ResetPasswordAsync(id, dto, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
