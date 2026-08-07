using Business.Abstract;
using Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Müştəri ilə saray arasındakı yazışmalara adminin nəzarəti.
    // Admin yazışmaya yaza bilmir və mesajı silə bilmir — yazışma mübahisədə
    // dəlildir. Yeganə müdaxilə: qanunsuz mesajı tərəflərdən gizlətmək
    // (mesaj bazada səbəbi ilə qalır, admin görməkdə davam edir).
    //
    // Baxışın özü jurnala düşür (AuditActionFilter): kimin hansı şəxsi
    // yazışmanı açdığı izlənilməlidir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class AdminChatsController : ControllerBase
    {
        private readonly IAdminChatService _service;

        public AdminChatsController(IAdminChatService service)
        {
            _service = service;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Açılmış yazışmaların siyahısı — saray, müştəri və mesaj mətni üzrə axtarışla
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AdminChatQueryDto query)
            => Ok(await _service.SearchAsync(query));

        // Bir yazışmanın bütün mesajları
        [HttpGet("{offerId}")]
        public async Task<IActionResult> Get(int offerId)
        {
            var result = await _service.GetAsync(offerId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        // Qanunsuz mesajı tərəflərdən gizlədir və ya yenidən göstərir.
        // Mesaj silinmir — admin onu görməkdə davam edir.
        [HttpPut("{messageId}")]
        public async Task<IActionResult> SetHidden(int messageId, [FromBody] SetMessageHiddenDto dto)
        {
            var result = await _service.SetHiddenAsync(messageId, dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
