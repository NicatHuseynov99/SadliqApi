using Business.Abstract;
using Entities.Concrete;
using Entities.Dtos.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Rəy moderasiyası. Dəstək admini burada platforma admini ilə eyni səlahiyyətdədir —
    // rəylərin SuperAdmin ilə əlaqəsi yoxdur, ona görə ayrıca məhdudiyyət lazım deyil.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class ReviewModerationController : ControllerBase
    {
        private readonly IReviewModerationService _service;

        public ReviewModerationController(IReviewModerationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var result = await _service.GetStatsAsync();
            return Ok(result);
        }

        // Baxanın rolu: düzəliş nişanı yalnız platforma sahibinə göndərilir
        private UserRole ViewerRole =>
            Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var role)
                ? role
                : UserRole.Support;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] ReviewSearchDto dto)
        {
            var result = await _service.SearchAsync(dto, ViewerRole);
            return Ok(result);
        }

        // Düzəliş tarixçəsi yalnız SuperAdmin üçündür
        [HttpGet("{reviewId}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetEdits(int reviewId)
        {
            var result = await _service.GetEditsAsync(reviewId);
            return Ok(result);
        }

        [HttpGet("{reviewId}")]
        public async Task<IActionResult> GetReports(int reviewId)
        {
            var result = await _service.GetReportsAsync(reviewId);
            return Ok(result);
        }

        [HttpPut("{reviewId}")]
        public async Task<IActionResult> SetHidden(int reviewId, [FromBody] SetReviewHiddenDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _service.SetHiddenAsync(reviewId, dto, adminId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{reviewId}")]
        public async Task<IActionResult> ResolveReports(int reviewId)
        {
            var result = await _service.ResolveReportsAsync(reviewId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{reviewId}")]
        public async Task<IActionResult> Update(int reviewId, [FromBody] EditReviewDto dto)
        {
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var adminName = User.FindFirstValue(ClaimTypes.Name) ?? "Admin";

            var result = await _service.UpdateAsync(reviewId, dto, adminId, adminName, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{reviewId}")]
        public async Task<IActionResult> Restore(int reviewId)
        {
            var result = await _service.RestoreAsync(reviewId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{reviewId}")]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var result = await _service.DeleteAsync(reviewId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
