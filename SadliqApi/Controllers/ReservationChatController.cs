using Business.Abstract;
using Entities.Dtos.Reservation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Birbaşa rezerv üzrə yazışma. Təklif söhbəti VenueRequestsController-də
    // qalır — bu, onun yanına qoyulan ikinci kanaldır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("write")]
    public class ReservationChatController : ControllerBase
    {
        private readonly IReservationChatService _chatService;

        public ReservationChatController(IReservationChatService chatService)
        {
            _chatService = chatService;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("{reservationId}")]
        public async Task<IActionResult> GetMessages(int reservationId)
        {
            var result = await _chatService.GetMessagesAsync(reservationId, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{reservationId}")]
        public async Task<IActionResult> SendMessage(
            int reservationId, [FromBody] SendReservationMessageDto dto)
        {
            var result = await _chatService.SendMessageAsync(reservationId, dto.Body, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
