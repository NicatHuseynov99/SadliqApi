using Business.Abstract;
using Entities.Dtos.VenueRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Tərs axtarış: müştəri şərtlərini paylaşır, saraylar təklif göndərir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("write")]
    public class VenueRequestsController : ControllerBase
    {
        private readonly IVenueRequestService _service;

        public VenueRequestsController(IVenueRequestService service)
        {
            _service = service;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ————— müştəri —————

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVenueRequestDto dto)
        {
            var result = await _service.CreateAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> GetMine()
            => Ok(await _service.GetMineAsync(UserId));

        [Authorize(Roles = "Customer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Close(int id)
        {
            var result = await _service.CloseAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> AcceptOffer(int id)
        {
            var result = await _service.AcceptOfferAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> DeclineOffer(int id)
        {
            var result = await _service.DeclineOfferAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ————— saray —————

        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> GetOpen()
            => Ok(await _service.GetOpenForOwnerAsync(UserId));

        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpPost]
        public async Task<IActionResult> CreateOffer([FromBody] CreateVenueOfferDto dto)
        {
            var result = await _service.CreateOfferAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Sahibin göndərdiyi təkliflər və yazışmalar
        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> GetMyOffers()
            => Ok(await _service.GetMyOffersAsync(UserId));

        // ————— yazışma —————

        // Müştəri təklifi təsdiqləyir və söhbət açılır (rezerv hələ yaranmır)
        [Authorize(Roles = "Customer")]
        [HttpPut("{id}")]
        public async Task<IActionResult> ApproveContact(int id)
        {
            var result = await _service.ApproveContactAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var result = await _service.GetMessagesAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            var result = await _service.SendMessageAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> WithdrawOffer(int id)
        {
            var result = await _service.WithdrawOfferAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
