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

        // Rəy yazmaq üçün giriş kifayətdir; sayına məhdudiyyət yoxdur
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
