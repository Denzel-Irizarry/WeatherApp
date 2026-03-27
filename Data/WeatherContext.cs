using Microsoft.EntityFrameworkCore;

namespace WeatherApp.Data;

public sealed class WeatherContext(DbContextOptions<WeatherContext> options) : DbContext(options)
{
    public DbSet<SavedLocation> SavedLocations => Set<SavedLocation>();
    public DbSet<ForecastSnapshot> ForecastSnapshots => Set<ForecastSnapshot>();
    public DbSet<ForecastPeriod> ForecastPeriods => Set<ForecastPeriod>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SavedLocation>(entity =>
        {
            entity.ToTable("SavedLocations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.Name).HasMaxLength(200);
            entity.HasIndex(location => new { location.Name, location.Latitude, location.Longitude }).IsUnique();
        });

        modelBuilder.Entity<ForecastSnapshot>(entity =>
        {
            entity.ToTable("ForecastSnapshots");
            entity.HasKey(snapshot => snapshot.Id);
            entity.Property(snapshot => snapshot.QueriedLocation).HasMaxLength(200);
            entity.Property(snapshot => snapshot.UnitsLabel).HasMaxLength(50);
            entity.Property(snapshot => snapshot.TemperatureUnit).HasMaxLength(10);
            entity.Property(snapshot => snapshot.WindSpeedUnit).HasMaxLength(10);

            entity.HasOne(snapshot => snapshot.SavedLocation)
                .WithMany(location => location.Snapshots)
                .HasForeignKey(snapshot => snapshot.SavedLocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ForecastPeriod>(entity =>
        {
            entity.ToTable("ForecastPeriods");
            entity.HasKey(period => period.Id);
            entity.Property(period => period.PeriodType).HasMaxLength(20);
            entity.Property(period => period.WeatherDescription).HasMaxLength(100);

            entity.HasOne(period => period.ForecastSnapshot)
                .WithMany(snapshot => snapshot.Periods)
                .HasForeignKey(period => period.ForecastSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(period => new { period.ForecastSnapshotId, period.PeriodType, period.Time });
        });
    }
}
