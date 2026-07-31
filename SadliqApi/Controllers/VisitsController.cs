using Business.Abstract;
using Entities.Dtos.Visits;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Ziyarətçi statistikası. Track hamıya açıqdır — qeydiyyatsız ziyarətçiləri
    // saymağın başqa yolu yoxdur. Hesabatlar isə yalnız admin tərəfə açıqdır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitService _visitService;

        public VisitsController(IVisitService visitService)
        {
            _visitService = visitService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Track([FromBody] TrackPageDto dto)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

            var result = await _visitService.TrackAsync(
                dto,
                userId,
                ResolveIpAddress(),
                Request.Headers.UserAgent.ToString());

            // Ziyarətçiyə heç vaxt xəta göstərmirik — statistika saytı dayandırmamalıdır
            return Ok(result);
        }

        // Sayt nginx / Cloudflare kimi proxy arxasında işləyəndə RemoteIpAddress
        // həmişə proxy-nin özünü göstərir. Ona görə əvvəlcə forward başlıqlarına baxırıq.
        private string? ResolveIpAddress()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                // "client, proxy1, proxy2" formatında gəlir — birincisi əsl ziyarətçidir
                var first = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(first)) return Normalize(first);
            }

            var realIp = Request.Headers["X-Real-IP"].ToString();
            if (!string.IsNullOrWhiteSpace(realIp)) return Normalize(realIp.Trim());

            return Normalize(HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        // IPv6-ya bükülmüş IPv4 ünvanını (::ffff:85.132.1.1) oxunaqlı hala salırıq
        private static string? Normalize(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return null;
            if (ip.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
                return ip["::ffff:".Length..];
            return ip;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Support")]
        public async Task<IActionResult> GetStats([FromQuery] int days = 14)
        {
            var result = await _visitService.GetStatsAsync(days);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Support")]
        public async Task<IActionResult> Search([FromQuery] VisitSearchDto dto)
        {
            var result = await _visitService.SearchAsync(dto);
            return Ok(result);
        }
    }
}
