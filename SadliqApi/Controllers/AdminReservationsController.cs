using Business.Abstract;
using Entities.Dtos.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Platforma üzrə rezerv idarəetməsi. Rezervlərin SuperAdmin ilə əlaqəsi olmadığı
    // üçün dəstək admini burada tam səlahiyyətlidir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class AdminReservationsController : ControllerBase
    {
        private readonly IAdminReservationService _adminReservations;
        private readonly IReservationService _reservations;

        public AdminReservationsController(
            IAdminReservationService adminReservations,
            IReservationService reservations)
        {
            _adminReservations = adminReservations;
            _reservations = reservations;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var result = await _adminReservations.GetStatsAsync();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AdminReservationSearchDto dto)
        {
            var result = await _adminReservations.SearchAsync(dto);
            return Ok(result);
        }

        [HttpPut("{reservationId}")]
        public async Task<IActionResult> Restore(int reservationId)
        {
            var result = await _adminReservations.RestoreAsync(reservationId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Mübahisəli hallarda admin sahibin yerinə qərar verir
        [HttpPut("{reservationId}")]
        public async Task<IActionResult> UpdateStatus(int reservationId, [FromBody] UpdateReservationStatusDto dto)
        {
            var adminUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservations.AdminUpdateStatusAsync(reservationId, dto, adminUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
