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

        // ?role=Owner&q=ali&status=blocked ilə süzmək olar
        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] string? role, [FromQuery] string? q, [FromQuery] string? status)
        {
            var result = await _adminService.GetUsersAsync(role, q, status, ViewerRole);
            return Ok(result);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserDetail(int userId)
        {
            var result = await _adminService.GetUserDetailAsync(userId, ViewerRole);
            if (!result.Success)
                return NotFound(result);
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

        [HttpPut("{userId}")]
        public async Task<IActionResult> SetUserActive(int userId, [FromBody] SetUserActiveDto dto)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _adminService.SetUserActiveAsync(userId, dto, currentAdminId, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> ResetUserPassword(int userId, [FromBody] AdminResetPasswordDto dto)
        {
            var result = await _adminService.ResetUserPasswordAsync(userId, dto, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var result = await _adminService.CreateUserAsync(dto, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserDto dto)
        {
            var result = await _adminService.UpdateUserAsync(userId, dto, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> RestoreUser(int userId)
        {
            var result = await _adminService.RestoreUserAsync(userId, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var currentAdminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _adminService.DeleteUserAsync(userId, currentAdminId, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
