using Business.Abstract;
using Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SadliqApi.Controllers
{
    // Müştəri ilə saray arasındakı yazışmalara adminin nəzarəti.
    // Yalnız oxumaq üçündür: yazmaq və silmək qəsdən yoxdur, çünki yazışma
    // mübahisədə dəlildir — dəyişdirilə bilməməlidir.
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
    }
}
