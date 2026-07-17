using Business.Abstract;
using Entities.Dtos.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        // Restoran sahibinin yeni menyu paketi əlavə etməsi
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMenuDto dto)
        {
            var result = await _menuService.CreateAsync(dto);
            return Ok(result);
        }
    }
}
