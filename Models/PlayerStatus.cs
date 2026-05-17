namespace VideoControlAPI.Models
{
    public class PlayerStatus
    {
        public string State { get; set; } = "stopped"; // playing, paused, stopped
        public Video? CurrentVideo { get; set; }
        public int QueueCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AddToQueueDto
    {
        public int VideoId { get; set; }
        public string RequestedBy { get; set; } = "Android";
    }
}
