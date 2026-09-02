using Microsoft.EntityFrameworkCore;
using NetPulseCore.Models;

namespace NetPulseCore.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<NodeEntity> Nodes => Set<NodeEntity>();
    public DbSet<JobTask> Jobs => Set<JobTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NodeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NodeName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Region).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.WorkloadType).HasMaxLength(50);
        });

        modelBuilder.Entity<JobTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.JobType).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(30);
        });
    }
}
