using Business.Abstract;
using Entities.Concrete;
using Entities.Dtos.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SadliqApi.Controllers
{
    // Bazanın birbaşa idarəsi. Buradan hər cədvələ, o cümlədən hesablara və jurnala
    // toxunmaq olur — ona görə YALNIZ platforma sahibi üçündür.
    // Dəstək admini bu bölməni nə görür, nə də sorğu ata bilir.
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "SuperAdmin")]
    public class DataController : ControllerBase
    {
        private readonly IDataExplorerService _service;

        public DataController(IDataExplorerService service)
        {
            _service = service;
        }

        // Bəzi cədvəllər yalnız platforma sahibinə açıqdır
        private UserRole ViewerRole =>
            Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), true, out var role)
                ? role
                : UserRole.Support;

        [HttpGet]
        public async Task<IActionResult> GetTables()
        {
            var result = await _service.GetTablesAsync(ViewerRole);
            return Ok(result);
        }

        [HttpGet("{table}")]
        public async Task<IActionResult> GetRows(
            string table,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            [FromQuery] string? sort = null,
            [FromQuery] bool desc = true,
            [FromQuery] bool includeDeleted = false)
        {
            var result = await _service.GetRowsAsync(table, q, page, pageSize, sort, desc, includeDeleted, ViewerRole);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("{table}")]
        public async Task<IActionResult> CreateRow(string table, [FromBody] TableRowDto dto)
        {
            var result = await _service.CreateRowAsync(table, dto, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{table}/{id}")]
        public async Task<IActionResult> UpdateRow(string table, string id, [FromBody] TableRowDto dto)
        {
            var result = await _service.UpdateRowAsync(table, id, dto, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{table}/{id}")]
        public async Task<IActionResult> DeleteRow(string table, string id)
        {
            var result = await _service.DeleteRowAsync(table, id, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{table}/{id}")]
        public async Task<IActionResult> RestoreRow(string table, string id)
        {
            var result = await _service.RestoreRowAsync(table, id, ViewerRole);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
