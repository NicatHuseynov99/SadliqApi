using Business.Abstract;
using Entities.Concrete;
using Entities.Dtos.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Fəaliyyət jurnalı: SuperAdmin hər şeyi, dəstək admini isə SuperAdmin-dən başqa hamını görür.
    // Jurnalı redaktə və silmə əməliyyatı qəsdən yoxdur — iz dəyişdirilə bilməməlidir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        private UserRole ViewerRole =>
            Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var role)
                ? role
                : UserRole.Support;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] AuditSearchDto dto)
        {
            var result = await _auditService.SearchAsync(dto, ViewerRole);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilterOptions()
        {
            var result = await _auditService.GetFilterOptionsAsync(ViewerRole);
            return Ok(result);
        }
    }
}
