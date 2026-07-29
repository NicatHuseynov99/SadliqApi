using Business.Abstract;
using Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Yalnız platformanın sahibi (SuperAdmin)
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var result = await _adminService.GetStatsAsync();
            return Ok(result);
        }

        // ?role=Owner ilə süzmək olar
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            var result = await _adminService.GetUsersAsync(role);
            return Ok(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> ChangeRole(int userId, [FromBody] ChangeRoleDto dto)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _adminService.ChangeRoleAsync(userId, dto, currentAdminId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
