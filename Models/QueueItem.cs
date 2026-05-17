using System.ComponentModel.DataAnnotations.Schema;

namespace VideoControlAPI.Models
{
    public class QueueItem
    {
        public int Id { get; set; }
        public int VideoId { get; set; }
        public int Position { get; set; }
        public bool IsPlaying { get; set; } = false;
        public bool IsFinished { get; set; } = false;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;

        [ForeignKey("VideoId")]
        public virtual Video? Video { get; set; }
    }
}
