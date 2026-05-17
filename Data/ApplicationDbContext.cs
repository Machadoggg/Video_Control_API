using Microsoft.EntityFrameworkCore;
using VideoControlAPI.Models;

namespace VideoControlAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Video> Videos { get; set; }
        public DbSet<QueueItem> Queue { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QueueItem>()
                .HasOne(q => q.Video)
                .WithMany()
                .HasForeignKey(q => q.VideoId);
        }
    }
}
