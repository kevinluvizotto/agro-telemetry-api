using Microsoft.EntityFrameworkCore;

namespace AgroTelemetry.Api.Data;

public class TelemetryDbContext(DbContextOptions<TelemetryDbContext> options) : DbContext(options)
{
    public DbSet<TelemetryReading> Readings => Set<TelemetryReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("telemetry");

        modelBuilder.Entity<TelemetryReading>(e =>
        {
            e.ToTable("Readings");
            e.HasKey(x => x.Id);
        });
    }
}

public class TelemetryReading
{
    public Guid Id { get; set; }
    public Guid PlotId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public decimal SoilMoisture { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal PrecipitationMm { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}