using Microsoft.EntityFrameworkCore;
using NetPulseCore.Data;
using NetPulseCore.Endpoints;
using NetPulseCore.Models;
using NetPulseCore.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=netpulse.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add Singleton and Hosted Services
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddSingleton<EventBroadcaster>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();
builder.Services.AddHostedService<JobProcessorWorker>();

// Configure OpenAPI & Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "⚡ NetPulse Core API",
        Version = "v1",
        Description = "Enterprise-Grade High-Throughput .NET 8 Async Microservice Platform, Background Task Engine & Telemetry Mesh."
    });
});

// Configure CORS for local development and SPA frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Ensure SQLite Database is initialized and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Nodes.Any())
    {
        db.Nodes.AddRange(
            new NodeEntity
            {
                NodeName = "core-gateway-alpha",
                Region = "eu-central-1",
                WorkloadType = "Ingress",
                CpuCores = 16,
                TotalMemoryMb = 32768,
                Status = "Active",
                LoadAverage = 0.18
            },
            new NodeEntity
            {
                NodeName = "compute-worker-01",
                Region = "us-east-1",
                WorkloadType = "AI-Worker",
                CpuCores = 32,
                TotalMemoryMb = 65536,
                Status = "Active",
                LoadAverage = 0.42
            },
            new NodeEntity
            {
                NodeName = "storage-edge-node",
                Region = "ap-southeast-1",
                WorkloadType = "Storage",
                CpuCores = 8,
                TotalMemoryMb = 16384,
                Status = "Active",
                LoadAverage = 0.08
            }
        );
        db.SaveChanges();
    }
}

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetPulse Core API v1");
    c.RoutePrefix = "swagger";
});

// Serve modern embedded web dashboard from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Map REST & SSE API endpoints
app.MapNetPulseEndpoints();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

Console.WriteLine("===============================================================");
Console.WriteLine("⚡ NetPulse Core (.NET 8 Enterprise Platform) is LIVE!");
Console.WriteLine("📊 Web Dashboard: http://localhost:5055");
Console.WriteLine("📖 Swagger Docs:  http://localhost:5055/swagger");
Console.WriteLine("===============================================================");

app.Run();
