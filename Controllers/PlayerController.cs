using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoControlAPI.Data;
using VideoControlAPI.Models;
using VideoControlAPI.Services;

namespace VideoControlAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly VlcService _vlc;

        public PlayerController(ApplicationDbContext db, VlcService vlc)
        {
            _db = db;
            _vlc = vlc;
        }

        // GET api/Player/status
        [HttpGet("status")]
        public async Task<ActionResult<PlayerStatus>> GetStatus()
        {
            var state = await _vlc.GetState();

            var current = await _db.Queue
                .Include(q => q.Video)
                .Where(q => q.IsPlaying && !q.IsFinished)
                .FirstOrDefaultAsync();

            var queueCount = await _db.Queue.CountAsync(q => !q.IsFinished && !q.IsPlaying);

            return Ok(new PlayerStatus
            {
                State = state,
                CurrentVideo = current?.Video,
                QueueCount = queueCount,
                Message = state == "playing"
                    ? $"Reproduciendo: {current?.Video?.Title ?? "..."}"
                    : state == "paused" ? "Pausado" : "Detenido"
            });
        }

        // POST api/Player/pause
        [HttpPost("pause")]
        public async Task<ActionResult> Pause()
        {
            var ok = await _vlc.Pause();
            return Ok(new { success = ok, message = ok ? "Pausado/Reanudado" : "Error al pausar" });
        }

        // POST api/Player/stop
        [HttpPost("stop")]
        public async Task<ActionResult> Stop()
        {
            // Marcar como terminado en BD
            var current = await _db.Queue
                .Where(q => q.IsPlaying && !q.IsFinished)
                .FirstOrDefaultAsync();

            if (current != null)
            {
                current.IsPlaying = false;
                current.IsFinished = true;
                await _db.SaveChangesAsync();
            }

            var ok = await _vlc.Stop();
            return Ok(new { success = ok, message = "Detenido" });
        }

        // POST api/Player/next
        [HttpPost("next")]
        public async Task<ActionResult> Next()
        {
            // Reutiliza la lógica de QueueController.PlayNext
            var current = await _db.Queue
                .Where(q => q.IsPlaying && !q.IsFinished)
                .FirstOrDefaultAsync();

            if (current != null)
            {
                current.IsPlaying = false;
                current.IsFinished = true;
            }

            var next = await _db.Queue
                .Include(q => q.Video)
                .Where(q => !q.IsFinished && !q.IsPlaying)
                .OrderBy(q => q.Position)
                .FirstOrDefaultAsync();

            if (next == null)
            {
                await _db.SaveChangesAsync();
                await _vlc.Stop();
                return Ok(new { message = "No hay más videos en la cola" });
            }

            next.IsPlaying = true;
            await _db.SaveChangesAsync();
            await _vlc.PlayFile(next.Video!.FilePath);

            return Ok(new { message = $"Reproduciendo: {next.Video.Title}", video = next.Video });
        }

        // POST api/Player/volume/{value}   (0-200)
        [HttpPost("volume/{value}")]
        public async Task<ActionResult> SetVolume(int value)
        {
            var ok = await _vlc.SetVolume(value);
            return Ok(new { success = ok, volume = value });
        }

        // POST api/Player/fullscreen
        [HttpPost("fullscreen")]
        public async Task<ActionResult> Fullscreen()
        {
            var ok = await _vlc.ToggleFullscreen();
            return Ok(new { success = ok });
        }
    }
}
