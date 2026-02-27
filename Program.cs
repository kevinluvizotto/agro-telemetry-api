using AgroTelemetry.Api.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Rabbit
var rabbitHost = builder.Configuration["Rabbit:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["Rabbit:User"] ?? "agro";
var rabbitPass = builder.Configuration["Rabbit:Pass"] ?? "agro";

// DB (schema telemetry)
builder.Services.AddDbContext<TelemetryDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "telemetry")
                  .EnableRetryOnFailure()
    )
);

// MassTransit (publisher)
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "AgroTelemetry API", Version = "v1" });
});

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/telemetry/readings", async (
    Guid plotId,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? take,
    TelemetryDbContext db) =>
{
    var q = db.Readings.Where(r => r.PlotId == plotId);

    if (from.HasValue) q = q.Where(r => r.Timestamp >= from.Value);
    if (to.HasValue) q = q.Where(r => r.Timestamp <= to.Value);

    var limit = Math.Clamp(take ?? 500, 1, 5000);

    var items = await q
        .OrderBy(r => r.Timestamp)
        .Take(limit)
        .Select(r => new
        {
            r.PlotId,
            r.Timestamp,
            r.SoilMoisture,
            r.TemperatureC,
            r.PrecipitationMm
        })
        .ToListAsync();

    return Results.Ok(items);
});

// DTO local (pode virar record depois)
app.MapPost("/telemetry/readings", async (
    TelemetryReadingRequest req,
    TelemetryDbContext db,
    IPublishEndpoint publish) =>
{
    var entity = new TelemetryReading
    {
        Id = Guid.NewGuid(),
        PlotId = req.PlotId,
        Timestamp = req.Timestamp,
        SoilMoisture = req.SoilMoisture,
        TemperatureC = req.TemperatureC,
        PrecipitationMm = req.PrecipitationMm,
        ReceivedAt = DateTimeOffset.UtcNow
    };

    db.Readings.Add(entity);
    await db.SaveChangesAsync();

    // Publish (contrato no namespace AgroAlerts.Api.Messaging)
    await publish.Publish(new AgroAlerts.Api.Messaging.SensorReadingReceived(
        req.PlotId,
        req.Timestamp,
        req.SoilMoisture,
        req.TemperatureC,
        req.PrecipitationMm
    ));

    return Results.Accepted($"/telemetry/readings/{entity.Id}", new { entity.Id });
});

app.Run();

public record TelemetryReadingRequest(
    Guid PlotId,
    DateTimeOffset Timestamp,
    decimal SoilMoisture,
    decimal TemperatureC,
    decimal PrecipitationMm
);