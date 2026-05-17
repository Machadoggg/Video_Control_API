namespace VideoControlAPI.Models
{
    public class Video
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public string FileSizeFormatted =>
            FileSizeBytes > 1_073_741_824
                ? $"{FileSizeBytes / 1_073_741_824.0:F1} GB"
                : $"{FileSizeBytes / 1_048_576.0:F1} MB";
    }
}
