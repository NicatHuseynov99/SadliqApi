using Business.Abstract;
using Entities.Dtos.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MenusController : ControllerBase
    {
        private readonly IMenuService _menuService;

        public MenusController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        // Bir sarayın bütün menyu paketlərini (50/60/70 AZN və s.) tərkibi ilə qaytarır
        [HttpGet("{venueId}")]
        public async Task<IActionResult> GetByVenue(int venueId)
        {
            var result = await _menuService.GetByVenueIdAsync(venueId);
            return Ok(result);
        }

        // Tək menyunun tərkibi ilə birgə detalları
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _menuService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        // Restoran sahibinin yeni menyu paketi əlavə etməsi
        [Authorize(Roles = "Owner")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.CreateAsync(dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Restoran sahibinin öz menyusunu redaktə etməsi
        [Authorize(Roles = "Owner")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMenuDto dto)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.UpdateAsync(id, dto, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        // Restoran sahibinin öz menyusunu silməsi
        [Authorize(Roles = "Owner")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _menuService.DeleteAsync(id, ownerId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
