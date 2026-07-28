using Business.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;

        public FavoritesController(IFavoriteService favoriteService)
        {
            _favoriteService = favoriteService;
        }

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Müştərinin seçilmiş saraylarının tam siyahısı
        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var result = await _favoriteService.GetMineAsync(CurrentUserId);
            return Ok(result);
        }

        // Yalnız Id-lər — ana səhifədə ürəklərin vəziyyətini göstərmək üçün
        [HttpGet]
        public async Task<IActionResult> GetMyVenueIds()
        {
            var result = await _favoriteService.GetMyVenueIdsAsync(CurrentUserId);
            return Ok(result);
        }

        // Ürəyə basanda çağırılır: seçilmişdə varsa silir, yoxdursa əlavə edir
        [HttpPost("{venueId}")]
        public async Task<IActionResult> Toggle(int venueId)
        {
            var result = await _favoriteService.ToggleAsync(venueId, CurrentUserId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
