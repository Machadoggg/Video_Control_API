using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoControlAPI.Data;
using VideoControlAPI.Models;
using VideoControlAPI.Services;

namespace VideoControlAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueueController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly VlcService _vlc;

        public QueueController(ApplicationDbContext db, VlcService vlc)
        {
            _db = db;
            _vlc = vlc;
        }

        // GET api/Queue
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QueueItem>>> GetQueue()
        {
            var queue = await _db.Queue
                .Include(q => q.Video)
                .Where(q => !q.IsFinished)
                .OrderBy(q => q.Position)
                .ToListAsync();
            return Ok(queue);
        }

        // GET api/Queue/count
        [HttpGet("count")]
        public ActionResult GetCount()
        {
            var count = _db.Queue.Count(q => !q.IsFinished);
            return Ok(new { count });
        }

        // POST api/Queue/add
        [HttpPost("add")]
        public async Task<ActionResult> AddToQueue([FromBody] AddToQueueDto dto)
        {
            var video = await _db.Videos.FindAsync(dto.VideoId);
            if (video == null)
                return NotFound(new { message = "Video no encontrado" });

            var alreadyQueued = await _db.Queue
                .AnyAsync(q => q.VideoId == dto.VideoId && !q.IsFinished);

            if (alreadyQueued)
                return BadRequest(new { message = "Este video ya está en la cola" });

            var maxPos = await _db.Queue
                .Where(q => !q.IsFinished)
                .MaxAsync(q => (int?)q.Position) ?? 0;

            var item = new QueueItem
            {
                VideoId = dto.VideoId,
                Position = maxPos + 1,
                RequestedBy = dto.RequestedBy,
                AddedAt = DateTime.UtcNow,
                IsPlaying = false,
                IsFinished = false
            };

            _db.Queue.Add(item);
            await _db.SaveChangesAsync();

            return Ok(new { message = $"'{video.Title}' agregado a la cola", position = item.Position });
        }

        // DELETE api/Queue/remove/{id}
        [HttpDelete("remove/{id}")]
        public async Task<ActionResult> RemoveFromQueue(int id)
        {
            var item = await _db.Queue.FindAsync(id);
            if (item == null) return NotFound();

            _db.Queue.Remove(item);

            // Reordenar
            var rest = await _db.Queue
                .Where(q => !q.IsFinished && q.Position > item.Position)
                .ToListAsync();
            foreach (var q in rest) q.Position--;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Video eliminado de la cola" });
        }

        // POST api/Queue/play-next  → marca como reproduciendo y lanza VLC
        [HttpPost("play-next")]
        public async Task<ActionResult<Video>> PlayNext()
        {
            // Marcar el actual como terminado
            var current = await _db.Queue
                .Where(q => q.IsPlaying && !q.IsFinished)
                .FirstOrDefaultAsync();

            if (current != null)
            {
                current.IsPlaying = false;
                current.IsFinished = true;
            }

            // Obtener el siguiente
            var next = await _db.Queue
                .Include(q => q.Video)
                .Where(q => !q.IsFinished && !q.IsPlaying)
                .OrderBy(q => q.Position)
                .FirstOrDefaultAsync();

            if (next == null)
            {
                await _db.SaveChangesAsync();
                await _vlc.Stop();
                return NotFound(new { message = "No hay más videos en la cola" });
            }

            next.IsPlaying = true;
            await _db.SaveChangesAsync();

            // Reproducir en VLC
            await _vlc.PlayFile(next.Video!.FilePath);

            return Ok(next.Video);
        }

        // GET api/Queue/now-playing
        [HttpGet("now-playing")]
        public async Task<ActionResult<QueueItem>> GetNowPlaying()
        {
            var item = await _db.Queue
                .Include(q => q.Video)
                .Where(q => q.IsPlaying && !q.IsFinished)
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "No hay video reproduciéndose" });

            return Ok(item);
        }

        // DELETE api/Queue/clear
        [HttpDelete("clear")]
        public async Task<ActionResult> ClearQueue()
        {
            var pending = await _db.Queue.Where(q => !q.IsFinished).ToListAsync();
            _db.Queue.RemoveRange(pending);
            await _db.SaveChangesAsync();
            await _vlc.Stop();
            return Ok(new { message = "Cola limpiada" });
        }
    }
}
