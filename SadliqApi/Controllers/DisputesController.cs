using Business.Abstract;
using Entities.Dtos.Dispute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Şikayət/mübahisə işləri.
    //
    // Rol bölgüsü: dəstək admini (Support) bu növbənin sahibidir — işi götürür,
    // tərəflərlə danışır və qərar verir. SuperAdmin də edə bilir, amma onun əsas
    // işi platforma konfiqurasiyası, rollar və bazadır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class DisputesController : ControllerBase
    {
        private readonly IDisputeService _disputes;

        public DisputesController(IDisputeService disputes)
        {
            _disputes = disputes;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // ————— tərəflər —————

        // Müştəri saraydan, saray müştəridən şikayət edir
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDisputeDto dto)
        {
            var result = await _disputes.CreateAsync(dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Öz şikayətlərim (açdığım və ya tərəfi olduğum)
        [HttpGet]
        public async Task<IActionResult> GetMine()
            => Ok(await _disputes.GetMineAsync(UserId));

        // İşin tam görünüşü — tərəf öz işini, dəstək istənilən işi görür
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var result = await _disputes.GetAsync(id, UserId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] SendDisputeMessageDto dto)
        {
            var result = await _disputes.SendMessageAsync(id, dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ————— dəstək növbəsi —————

        [Authorize(Roles = "SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] DisputeQueryDto query)
            => Ok(await _disputes.SearchAsync(query));

        // İşi öz üzərinə götürmək
        [Authorize(Roles = "SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Assign(int id)
        {
            var result = await _disputes.AssignAsync(id, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Qərar vermək — izah məcburidir, tərəflər onu görür
        [Authorize(Roles = "SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Resolve(int id, [FromBody] ResolveDisputeDto dto)
        {
            var result = await _disputes.ResolveAsync(id, dto, UserId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
