using HappyDay.Application.Common.Exceptions;
using HappyDay.Application.Interface.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class FileService : IFileService
    {
        // Yüklemeler wwwroot'tan herkese sunulduğu için yalnızca resim kabul edilir.
        // .svg bilerek yok: içine script gömülebilir.
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB; tüm istek Kestrel'de 15 MB ile sınırlı

        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FileService> _logger;

        public FileService(IWebHostEnvironment env, ILogger<FileService> logger)
        {
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Uzantıya, boyuta ve dosyanın ilk baytlarına bakar. Uzantı istemciden geldiği için tek başına
        /// yeterli değil: "resim.jpg" adıyla HTML ya da çalıştırılabilir dosya yüklenebilir.
        /// </summary>
        private static async Task<string> GetValidatedExtensionAsync(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName);

            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new FileValidationException(
                    $"Desteklenmeyen dosya türü: {file.FileName}. İzin verilenler: {string.Join(", ", AllowedExtensions)}");

            if (file.Length == 0)
                throw new FileValidationException($"Dosya boş: {file.FileName}");

            if (file.Length > MaxFileSize)
                throw new FileValidationException(
                    $"Dosya çok büyük: {file.FileName}. En fazla {MaxFileSize / (1024 * 1024)} MB yüklenebilir.");

            var head = new byte[12];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false);
                head = head[..read];
            }

            extension = extension.ToLowerInvariant();
            if (!MatchesSignature(extension, head))
                throw new FileValidationException($"Dosya içeriği uzantısıyla uyuşmuyor: {file.FileName}");

            return extension;
        }

        private static bool MatchesSignature(string extension, ReadOnlySpan<byte> head) => extension switch
        {
            ".jpg" or ".jpeg" => head.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            ".png" => head.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => head.StartsWith("GIF87a"u8) || head.StartsWith("GIF89a"u8),
            ".webp" => head.Length >= 12 && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8),
            _ => false
        };

        public async Task<List<string>> SaveFilesAsync(List<IFormFile> files, string targetFolder)
        {
            // Diske yazmadan önce hepsini kontrol et; biri hatalıysa hiçbiri kaydedilmesin
            var extensions = new List<string>(files.Count);
            foreach (var file in files)
                extensions.Add(await GetValidatedExtensionAsync(file));

            var savedPaths = new List<string>();
            var uploadPath = Path.Combine(_env.WebRootPath, targetFolder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var fileName = Guid.NewGuid() + extensions[i];
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                savedPaths.Add($"/{targetFolder}/{fileName}");
            }

            return savedPaths;
        }
        public async Task<string> SaveFileAsync(IFormFile file, string folder)
        {
            var extension = await GetValidatedExtensionAsync(file);

            // 1. Dosya klasörü oluşturulacak yol
            var uploadPath = Path.Combine(_env.WebRootPath, folder);

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            // 2. Dosya adını benzersiz yap
            var fileName = $"{Guid.NewGuid()}{extension}";

            // 3. Tam fiziksel yol
            var filePath = Path.Combine(uploadPath, fileName);

            // 4. Dosyayı fiziksel olarak yaz
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Web üzerinden erişilebilir yol döndür (örnek: /uploads/cover/abc.jpg)
            var relativePath = Path.Combine(folder, fileName).Replace("\\", "/");
            return "/" + relativePath;
        }

        public void DeleteFile(string? webPath)
        {
            // Yalnızca buraya yüklenen dosyalar silinir; örnek verideki http adresleri vb. atlanır
            if (string.IsNullOrWhiteSpace(webPath) || !webPath.StartsWith("/uploads/", StringComparison.Ordinal))
                return;

            var uploadsRoot = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(Path.Combine(_env.WebRootPath, webPath.TrimStart('/')));

            // "/uploads/../appsettings.json" gibi bir yol uploads klasörünün dışına çıkamasın
            if (!fullPath.StartsWith(uploadsRoot, StringComparison.Ordinal))
            {
                _logger.LogWarning("uploads dışındaki dosya silinmek istendi: {Path}", webPath);
                return;
            }

            try
            {
                File.Delete(fullPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Kayıt zaten güncellendi; silinemeyen dosya yalnızca yer kaplar
                _logger.LogWarning(ex, "Eski dosya silinemedi: {Path}", webPath);
            }
        }
    }
}
