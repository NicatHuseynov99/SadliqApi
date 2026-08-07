using Business.Abstract;
using Entities.Dtos.Review;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [EnableRateLimiting("write")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // Sarayın rəyləri — giriş tələb olunmur
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var result = await _reviewService.GetByVenueAsync(venueId);
            return Ok(result);
        }

        // Səhifə formanı göstərməzdən əvvəl soruşur
        [Authorize]
        [HttpGet("{venueId}")]
        public async Task<IActionResult> CanReview(int venueId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _reviewService.CanReviewAsync(venueId, userId));
        }

        // Rəy yalnız tədbiri keçmiş qonaqdan qəbul olunur
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Qonaq";

            var result = await _reviewService.CreateAsync(dto, userId, authorName);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // İstifadəçi öz rəyini redaktə edir
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reviewService.UpdateAsync(id, dto, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Uyğunsuz rəyi şikayət etmək — giriş tələb olunur ki, təkrar şikayət olmasın
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Report([FromBody] ReportReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reviewService.ReportAsync(dto, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Saray rəyə açıq cavab yazır. Bir rəyə bir cavab olur — təkrar
        // çağırış mövcud cavabı əvəz edir.
        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Reply(int id, [FromBody] ReplyReviewDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reviewService.ReplyAsync(id, dto, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Saray öz cavabını götürür
        [Authorize(Roles = "Owner,VenueAdmin,SuperAdmin,Support")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reviewService.DeleteReplyAsync(id, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // İstifadəçi öz rəyini silir
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _reviewService.DeleteAsync(id, userId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
