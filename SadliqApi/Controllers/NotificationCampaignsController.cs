using Business.Abstract;
using Entities.Dtos.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Bildiriş göndərişi. Admin və dəstək platforma üzrə, saray sahibi isə
    // yalnız öz qonaqlarına göndərə bilir — bu yoxlama servis qatındadır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support,Owner")]
    [EnableRateLimiting("write")]
    public class NotificationCampaignsController : ControllerBase
    {
        private readonly INotificationCampaignService _service;

        public NotificationCampaignsController(INotificationCampaignService service)
        {
            _service = service;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Göndərmədən əvvəl: neçə nəfərə gedəcək
        [HttpPost]
        public async Task<IActionResult> Preview([FromBody] AudienceDto audience)
        {
            var result = await _service.PreviewAsync(audience, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] CreateCampaignDto dto)
        {
            var result = await _service.SendAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Özünə sınaq göndərişi
        [HttpPost]
        public async Task<IActionResult> SendTest([FromBody] CreateCampaignDto dto)
        {
            var result = await _service.SendTestAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Support")]
        public async Task<IActionResult> Search([FromQuery] CampaignQueryDto query)
            => Ok(await _service.SearchAsync(query));

        [HttpGet("{id}")]
        [Authorize(Roles = "SuperAdmin,Support")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _service.GetAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "SuperAdmin,Support")]
        public async Task<IActionResult> Cancel(int id)
        {
            var result = await _service.CancelAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
