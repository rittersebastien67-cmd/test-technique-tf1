using Microsoft.EntityFrameworkCore;

namespace HrAdministration.Infrastructure.EventSourcing;

public sealed class HrAdministrationDbContext : DbContext
{
    public HrAdministrationDbContext(DbContextOptions<HrAdministrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<EventRecord> Events => Set<EventRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("events");

            // The key is the unique constraint on (stream_id, version) that optimistic concurrency relies on.
            entity.HasKey(record => new { record.StreamId, record.Version }).HasName("pk_events");

            entity.Property(record => record.StreamId).HasColumnName("stream_id");
            entity.Property(record => record.Version).HasColumnName("version");
            entity.Property(record => record.Type).HasColumnName("type");
            entity.Property(record => record.Data).HasColumnName("data").HasColumnType("jsonb");
            entity.Property(record => record.Metadata).HasColumnName("metadata").HasColumnType("jsonb");
            entity.Property(record => record.RecordedAt).HasColumnName("recorded_at");
        });
    }
}
