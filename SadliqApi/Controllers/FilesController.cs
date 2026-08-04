using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        [EnableRateLimiting("upload")]
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

            // Uzantı yalançı ola bilər: .jpg adlı fayl icra faylı da ola bilər.
            // Ona görə faylın ilk baytları — həqiqi imzası — yoxlanılır.
            if (!await HasValidSignatureAsync(file, isVideo))
                return BadRequest(new
                {
                    success = false,
                    message = "Faylın məzmunu adına uyğun deyil."
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

        // Faylın ilk baytları («magic bytes») formatın həqiqi göstəricisidir.
        // Şəkillər üçün dəqiq imzalar, videolar üçün ISO/WebM konteyner nişanları yoxlanılır.
        private static async Task<bool> HasValidSignatureAsync(IFormFile file, bool isVideo)
        {
            var head = new byte[16];

            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(head);
                if (read < 12) return false;
            }

            bool Starts(params byte[] signature) =>
                head.Take(signature.Length).SequenceEqual(signature);

            if (!isVideo)
            {
                return Starts(0xFF, 0xD8, 0xFF)                                      // jpg
                    || Starts(0x89, 0x50, 0x4E, 0x47)                                // png
                    || Starts(0x47, 0x49, 0x46, 0x38)                                // gif
                    || (Starts(0x52, 0x49, 0x46, 0x46)                               // webp: RIFF….WEBP
                        && head.Skip(8).Take(4).SequenceEqual(new byte[] { 0x57, 0x45, 0x42, 0x50 }));
            }

            // mp4/mov: 4–8-ci baytlarda «ftyp»; webm/mkv: EBML başlığı
            var isIsoContainer = head.Skip(4).Take(4)
                .SequenceEqual(new byte[] { 0x66, 0x74, 0x79, 0x70 });

            return isIsoContainer || Starts(0x1A, 0x45, 0xDF, 0xA3);
        }
    }
}
