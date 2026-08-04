using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SadliqApi.Controllers
{
    // Şəkil və video yükləmə. Sahibkar link axtarmasın deyə fayl birbaşa
    // kompüterindən götürülür, server onu saxlayır və linkini özü qaytarır.
    //
    // Giriş tələb olunmur: saray müraciəti hesabsız da doldurula bilər.
    // Əvəzində ölçü və format sərt yoxlanılır.
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IWebHostEnvironment environment, ILogger<FilesController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        private const long MaxImageBytes = 8 * 1024 * 1024;    // 8 MB
        private const long MaxVideoBytes = 100L * 1024 * 1024; // 100 MB

        private static readonly string[] ImageTypes = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private static readonly string[] VideoTypes = { ".mp4", ".webm", ".mov" };

        [HttpPost]
        [AllowAnonymous]
        [RequestSizeLimit(110L * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { success = false, message = "Fayl seçilməyib." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var isImage = ImageTypes.Contains(extension);
            var isVideo = VideoTypes.Contains(extension);

            if (!isImage && !isVideo)
                return BadRequest(new
                {
                    success = false,
                    message = "Yalnız şəkil (jpg, png, webp, gif) və video (mp4, webm, mov) yükləmək olar."
                });

            var limit = isVideo ? MaxVideoBytes : MaxImageBytes;
            if (file.Length > limit)
                return BadRequest(new
                {
                    success = false,
                    message = $"Fayl çox böyükdür. Ən çox {limit / 1024 / 1024} MB olmalıdır."
                });

            try
            {
                // Fayllar ay-ay qovluqlara bölünür ki, bir qovluqda minlərlə fayl yığılmasın
                var folder = Path.Combine("uploads", DateTime.UtcNow.ToString("yyyy-MM"));
                var root = Path.Combine(_environment.WebRootPath ?? "wwwroot", folder);
                Directory.CreateDirectory(root);

                // Ad təsadüfi verilir: eyni adlı fayllar bir-birini əvəz etməsin
                // və istifadəçinin fayl adı serverə düşməsin
                var name = $"{Guid.NewGuid():N}{extension}";
                var path = Path.Combine(root, name);

                await using (var stream = System.IO.File.Create(path))
                    await file.CopyToAsync(stream);

                var url = $"{Request.Scheme}://{Request.Host}/{folder.Replace('\\', '/')}/{name}";

                return Ok(new
                {
                    success = true,
                    message = "Fayl yükləndi.",
                    data = new { url, type = isVideo ? "video" : "image", size = file.Length }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fayl yüklənə bilmədi: {Name}", file.FileName);
                return StatusCode(500, new { success = false, message = "Fayl saxlanıla bilmədi." });
            }
        }
    }
}
