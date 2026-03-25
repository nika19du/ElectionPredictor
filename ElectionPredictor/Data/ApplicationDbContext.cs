using ElectionPredictor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionPredictor.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Party> Parties => Set<Party>();
        public DbSet<Election> Elections => Set<Election>();
        public DbSet<PollEntry> PollEntries => Set<PollEntry>();
        public DbSet<ElectionResult> ElectionResults => Set<ElectionResult>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Party>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ShortName).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<Election>(entity =>
            {
                entity.Property(x => x.Type).HasMaxLength(100).IsRequired();
                entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            });

            modelBuilder.Entity<PollEntry>(entity =>
            {
                entity.Property(x => x.Pollster).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Percentage).HasPrecision(5, 2);
                entity.Property(x => x.SourceUrl).HasMaxLength(500);
                entity.Property(x => x.ExternalKey).HasMaxLength(300).IsRequired();

                entity.HasIndex(x => x.ExternalKey).IsUnique();
            });

            modelBuilder.Entity<ElectionResult>(entity =>
            {
                entity.Property(x => x.VotePercentage).HasPrecision(5, 2);
            });
        }
    }
}