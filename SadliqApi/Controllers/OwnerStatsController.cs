using Business.Abstract;
using Entities.Dtos.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Sahibkarın öz sarayları üzrə hesabatı
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
    public class OwnerStatsController : ControllerBase
    {
        private readonly IOwnerStatsService _service;
        private readonly IAuditService _audit;
        private readonly IVenueAccessService _access;

        public OwnerStatsController(
            IOwnerStatsService service,
            IAuditService audit,
            IVenueAccessService access)
        {
            _service = service;
            _audit = audit;
            _access = access;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int days = 30, [FromQuery] int? venueId = null)
        {
            var result = await _service.GetAsync(CurrentUserId, days, venueId);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Sahibin öz sarayları üzrə fəaliyyət jurnalı: administratorunun nə etdiyi.
        // Yalnız saray sahibinə açıqdır — administrator öz üzərində nəzarəti görməməlidir.
        [Authorize(Roles = "Owner,SuperAdmin,Support")]
        [HttpGet]
        public async Task<IActionResult> ActivityLog([FromQuery] AuditSearchDto dto)
        {
            var venueIds = await _access.GetManagedVenueIdsAsync(CurrentUserId);
            var result = await _audit.SearchForOwnerAsync(dto, venueIds);

            return Ok(result);
        }
    }
}
