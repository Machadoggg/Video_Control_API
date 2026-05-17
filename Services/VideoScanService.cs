using VideoControlAPI.Data;
using VideoControlAPI.Models;

namespace VideoControlAPI.Services
{
    /// <summary>
    /// Escanea la carpeta de videos configurada y sincroniza la BD.
    /// </summary>
    public class VideoScanService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<VideoScanService> _logger;

        public VideoScanService(ApplicationDbContext db, IConfiguration config,
            ILogger<VideoScanService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        public void ScanAndSync()
        {
            var folder = _config["VideoSettings:VideoFolder"] ?? @"C:\Videos";
            var extStr = _config["VideoSettings:SupportedExtensions"] ?? ".mp4,.avi,.mkv,.mov,.wmv";
            var extensions = extStr.Split(',').Select(e => e.Trim().ToLower()).ToHashSet();

            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("Carpeta de videos no existe: {Folder}", folder);
                Directory.CreateDirectory(folder);
                return;
            }

            var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                .ToList();

            _logger.LogInformation("Encontrados {Count} videos en {Folder}", files.Count, folder);

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var exists = _db.Videos.Any(v => v.FilePath == filePath);
                if (exists) continue;

                var info = new FileInfo(filePath);
                // Título: nombre sin extensión, guiones/underscores → espacios
                var title = Path.GetFileNameWithoutExtension(fileName)
                    .Replace("_", " ").Replace("-", " ");

                _db.Videos.Add(new Video
                {
                    Title = title,
                    FileName = fileName,
                    FilePath = filePath,
                    Extension = Path.GetExtension(filePath).ToLower(),
                    FileSizeBytes = info.Length,
                    Category = Path.GetDirectoryName(filePath) == folder
                        ? "General"
                        : new DirectoryInfo(Path.GetDirectoryName(filePath)!).Name,
                    IsActive = true,
                    AddedAt = DateTime.UtcNow
                });
            }

            _db.SaveChanges();
        }
    }
}
