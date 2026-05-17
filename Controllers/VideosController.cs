using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoControlAPI.Data;
using VideoControlAPI.Models;
using VideoControlAPI.Services;

namespace VideoControlAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly VideoScanService _scanner;

        public VideosController(ApplicationDbContext db, VideoScanService scanner)
        {
            _db = db;
            _scanner = scanner;
        }

        // GET api/Videos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Video>>> GetVideos()
        {
            var videos = await _db.Videos
                .Where(v => v.IsActive)
                .OrderBy(v => v.Category)
                .ThenBy(v => v.Title)
                .ToListAsync();
            return Ok(videos);
        }

        // GET api/Videos/search?query=matrix
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Video>>> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetVideos();

            var q = query.ToLower();
            var videos = await _db.Videos
                .Where(v => v.IsActive &&
                    (v.Title.ToLower().Contains(q) ||
                     v.FileName.ToLower().Contains(q) ||
                     v.Category.ToLower().Contains(q)))
                .OrderBy(v => v.Title)
                .ToListAsync();
            return Ok(videos);
        }

        // GET api/Videos/categories
        [HttpGet("categories")]
        public ActionResult<IEnumerable<string>> GetCategories()
        {
            var cats = _db.Videos
                .Where(v => v.IsActive)
                .Select(v => v.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            return Ok(cats);
        }

        // GET api/Videos/category/{cat}
        [HttpGet("category/{cat}")]
        public async Task<ActionResult<IEnumerable<Video>>> GetByCategory(string cat)
        {
            var videos = await _db.Videos
                .Where(v => v.IsActive && v.Category == cat)
                .OrderBy(v => v.Title)
                .ToListAsync();
            return Ok(videos);
        }

        // POST api/Videos/scan  → rescans folder
        [HttpPost("scan")]
        public ActionResult Scan()
        {
            _scanner.ScanAndSync();
            var count = _db.Videos.Count(v => v.IsActive);
            return Ok(new { message = $"Escaneo completo. {count} videos disponibles." });
        }

        // GET api/Videos/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Video>> GetVideo(int id)
        {
            var video = await _db.Videos.FindAsync(id);
            if (video == null) return NotFound();
            return Ok(video);
        }
    }
}
