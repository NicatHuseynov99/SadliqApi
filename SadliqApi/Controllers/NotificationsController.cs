using Business.Abstract;
using Entities.Dtos.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // İstifadəçinin öz bildirişləri: zəng siyahısı, oxundu işarəsi, seçimlər.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;

        public NotificationsController(INotificationService service)
        {
            _service = service;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetMine([FromQuery] NotificationQueryDto query)
            => Ok(await _service.GetFeedAsync(UserId, query));

        // Zəng ikonundakı rəqəm — tez-tez çağırıldığı üçün ayrıca və yüngül
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
            => Ok(await _service.GetUnreadCountAsync(UserId));

        [HttpPut("{id}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var result = await _service.MarkReadAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await _service.MarkAllReadAsync(UserId));

        // Bildirişdəki linkə keçid — elanın nəticəsini ölçmək üçün
        [HttpPut("{id}")]
        public async Task<IActionResult> RegisterClick(int id)
            => Ok(await _service.RegisterClickAsync(id, UserId));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetPreferences()
            => Ok(await _service.GetPreferencesAsync(UserId));

        [HttpPut]
        public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferenceDto dto)
            => Ok(await _service.UpdatePreferencesAsync(UserId, dto));
    }
}
