using Microsoft.EntityFrameworkCore;

namespace TravelAdvisor.Infrastructure.Persistence;

public sealed class TravelAdvisorDbContext(DbContextOptions<TravelAdvisorDbContext> options) : DbContext(options)
{
    public DbSet<District> Districts => Set<District>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<District>(entity =>
        {
            entity.ToTable("districts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .HasMaxLength(10);

            entity.Property(e => e.DivisionId)
                .HasColumnName("division_id")
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.BnName)
                .HasColumnName("bn_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Latitude)
                .HasColumnName("latitude")
                .IsRequired();

            entity.Property(e => e.Longitude)
                .HasColumnName("longitude")
                .IsRequired();

            entity.HasIndex(e => e.Name)
                .IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
