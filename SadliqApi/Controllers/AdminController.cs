using Business.Abstract;
using Entities.Concrete;
using Entities.Dtos.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Platforma idarəetməsi. Dəstək admini eyni panelə girir, amma SuperAdmin
    // hesablarını görmür və onlara toxuna bilmir — süzgəc servis qatındadır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin,Support")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        private UserRole ViewerRole =>
            Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var role)
                ? role
                : UserRole.Support;

        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            var result = await _adminService.GetStatsAsync(ViewerRole);
            return Ok(result);
        }

        // ?role=Owner ilə süzmək olar
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? role)
        {
            var result = await _adminService.GetUsersAsync(role, ViewerRole);
            return Ok(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> ChangeRole(int userId, [FromBody] ChangeRoleDto dto)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _adminService.ChangeRoleAsync(userId, dto, currentAdminId, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
