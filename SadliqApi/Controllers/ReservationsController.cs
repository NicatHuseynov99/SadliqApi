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
        [Authorize(Roles = "Customer")]
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
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.GetByCustomerAsync(customerId);
            return Ok(result);
        }

        // Sarayın rezerv sorğuları — sahib və ya həmin sarayın administratoru
        [Authorize(Roles = "Owner,VenueAdmin")]
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.GetByVenueAsync(venueId, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // İdarə edilən bütün sarayların rezervləri — panel tək sorğu ilə dolur
        [Authorize(Roles = "Owner,VenueAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetForOwner()
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.GetByOwnerAsync(ownerId);
            return Ok(result);
        }

        // Rezervin təsdiqi/rəddi — sahib və ya sarayın administratoru (Status: 0=Pending, 1=Confirmed, 2=Rejected, 3=Cancelled)
        [Authorize(Roles = "Owner,VenueAdmin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateReservationStatusDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.UpdateStatusAsync(id, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Müştərinin öz göndərdiyi rezerv sorğusunu ləğv etməsi
        [Authorize(Roles = "Customer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var customerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reservationService.CancelAsync(id, customerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
