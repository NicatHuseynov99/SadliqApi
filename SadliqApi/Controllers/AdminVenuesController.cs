using Business.Abstract;
using Entities.Dtos.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Platforma üzrə saray idarəetməsi. Sarayın məzmununu (ad, ünvan, zallar, menyular)
    // dəyişmək üçün adi VenuesController işlədilir — admin oraya da tam çıxışa malikdir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class AdminVenuesController : ControllerBase
    {
        private readonly IAdminVenueService _adminVenues;
        private readonly IVenueService _venues;

        public AdminVenuesController(IAdminVenueService adminVenues, IVenueService venues)
        {
            _adminVenues = adminVenues;
            _venues = venues;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AdminVenueSearchDto dto)
        {
            var result = await _adminVenues.SearchAsync(dto);
            return Ok(result);
        }

        [HttpPut("{venueId}")]
        public async Task<IActionResult> SetApproved(int venueId, [FromBody] SetVenueApprovedDto dto)
        {
            var result = await _adminVenues.SetApprovedAsync(venueId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{venueId}")]
        public async Task<IActionResult> ChangeOwner(int venueId, [FromBody] ChangeVenueOwnerDto dto)
        {
            var result = await _adminVenues.ChangeOwnerAsync(venueId, dto);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{venueId}")]
        public async Task<IActionResult> Restore(int venueId)
        {
            var result = await _adminVenues.RestoreAsync(venueId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Sarayı silmək: admin bütün saraylara sahib səlahiyyəti ilə çıxdığı üçün
        // adi silmə axını işləyir, bütün yoxlamalar orada qalır
        [HttpDelete("{venueId}")]
        public async Task<IActionResult> Delete(int venueId)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _venues.DeleteAsync(venueId, adminId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
