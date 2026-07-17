using Business.Abstract;
using Entities.Dtos.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservationService;

        public ReservationsController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // Müştəri zal + gün + menyu seçib rezerv sorğusu göndərir
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReservationDto dto)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.CreateAsync(dto, customerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Müştərinin öz göndərdiyi rezervlərin siyahısı
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.GetByCustomerAsync(customerId);
            return Ok(result);
        }

        // Restoran sahibinin öz sarayına gələn rezerv sorğularını görməsi
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.GetByVenueAsync(venueId, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Restoran sahibinin rezervi təsdiq/rədd etməsi (Status: 0=Pending, 1=Confirmed, 2=Rejected, 3=Cancelled)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReservationStatusDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.UpdateStatusAsync(id, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
