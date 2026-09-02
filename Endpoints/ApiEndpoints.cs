using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetPulseCore.Data;
using NetPulseCore.Models;
using NetPulseCore.Services;

namespace NetPulseCore.Endpoints;

public static class ApiEndpoints
{
    public static void MapNetPulseEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");

        // System & Overview
        api.MapGet("/overview", async (AppDbContext db, ITelemetryService telemetry) =>
        {
            var sw = Stopwatch.StartNew();
            var metrics = await telemetry.GetMetricsAsync();
            var totalNodes = await db.Nodes.CountAsync();
            var activeNodes = await db.Nodes.CountAsync(n => n.Status == "Active");
            var completedJobs = await db.Jobs.CountAsync(j => j.Status == "Completed");
            var runningJobs = await db.Jobs.CountAsync(j => j.Status == "Processing" || j.Status == "Queued");

            var summary = new ClusterOverviewSummary
            {
                EngineName = "NetPulse Core (.NET 8 Enterprise)",
                Status = "Operational",
                TotalNodes = totalNodes,
                HealthyNodes = activeNodes,
                TotalJobsProcessed = completedJobs,
                ActiveJobsRunning = runningJobs,
                SystemCpuPercent = metrics.ProcessCpuPercent,
                WorkingSetMemoryMb = metrics.WorkingSetMemoryMb,
                ProcessUptime = metrics.ProcessUptime,
                ServerTimeUtc = DateTime.UtcNow
            };
            sw.Stop();

            return Results.Ok(new ApiResponse<ClusterOverviewSummary>(true, summary, "Cluster overview metrics retrieved.", sw.ElapsedMilliseconds));
        }).WithTags("Overview").WithSummary("Get cluster overview summary");

        // Telemetry Endpoints
        api.MapGet("/telemetry/live", async (ITelemetryService telemetry) =>
        {
            var sw = Stopwatch.StartNew();
            var metrics = await telemetry.GetMetricsAsync();
            sw.Stop();
            return Results.Ok(new ApiResponse<TelemetryMetrics>(true, metrics, "Live telemetry polled.", sw.ElapsedMilliseconds));
        }).WithTags("Telemetry").WithSummary("Poll live runtime & hardware telemetry");

        api.MapGet("/telemetry/stream", async (HttpContext context, EventBroadcaster broadcaster, CancellationToken ct) =>
        {
            context.Response.Headers.Append("Content-Type", "text/event-stream");
            context.Response.Headers.Append("Cache-Control", "no-cache");
            context.Response.Headers.Append("Connection", "keep-alive");

            var subId = broadcaster.Subscribe(out var reader);
            try
            {
                await context.Response.WriteAsync("data: {\"event\":\"connected\",\"message\":\"NetPulse SSE Telemetry Stream Active\"}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);

                while (!ct.IsCancellationRequested)
                {
                    if (await reader.WaitToReadAsync(ct))
                    {
                        while (reader.TryRead(out var msg))
                        {
                            await context.Response.WriteAsync($"data: {msg}\n\n", ct);
                            await context.Response.Body.FlushAsync(ct);
                        }
                    }
                }
            }
            finally
            {
                broadcaster.Unsubscribe(subId);
            }
        }).WithTags("Telemetry").WithSummary("Server-Sent Events (SSE) live event stream");

        api.MapGet("/health", async (AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            bool dbHealthy = await db.Database.CanConnectAsync();
            sw.Stop();

            var healthData = new
            {
                status = dbHealthy ? "Healthy" : "Degraded",
                framework = ".NET 8.0 Minimal APIs",
                database = dbHealthy ? "SQLite (Connected)" : "Disconnected",
                latencyMs = sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            };

            return Results.Ok(new ApiResponse<object>(true, healthData, "Health status checked.", sw.ElapsedMilliseconds));
        }).WithTags("Health").WithSummary("Service and database health check");

        // Node Management Endpoints
        var nodes = api.MapGroup("/nodes").WithTags("Cluster Nodes");

        nodes.MapGet("/", async (AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            var list = await db.Nodes.OrderByDescending(n => n.CreatedAt).ToListAsync();
            sw.Stop();
            return Results.Ok(new ApiResponse<List<NodeEntity>>(true, list, "Cluster nodes list retrieved.", sw.ElapsedMilliseconds));
        }).WithSummary("List all cluster nodes");

        nodes.MapPost("/", async (AppDbContext db, [FromBody] CreateNodeRequest req, EventBroadcaster broadcaster) =>
        {
            var sw = Stopwatch.StartNew();
            var node = new NodeEntity
            {
                NodeName = string.IsNullOrWhiteSpace(req.NodeName) ? $"node-{Guid.NewGuid().ToString("N")[..6]}" : req.NodeName,
                Region = req.Region,
                WorkloadType = req.WorkloadType,
                CpuCores = req.CpuCores > 0 ? req.CpuCores : Environment.ProcessorCount,
                TotalMemoryMb = req.TotalMemoryMb > 0 ? req.TotalMemoryMb : 16384,
                Status = "Active",
                LoadAverage = Math.Round(new Random().NextDouble() * 0.4 + 0.05, 2)
            };

            db.Nodes.Add(node);
            await db.SaveChangesAsync();

            await broadcaster.BroadcastAsync(JsonSerializer.Serialize(new { eventType = "node_registered", node }), CancellationToken.None);

            sw.Stop();
            return Results.Created($"/api/v1/nodes/{node.Id}", new ApiResponse<NodeEntity>(true, node, "Cluster node successfully registered.", sw.ElapsedMilliseconds));
        }).WithSummary("Register a new cluster node");

        nodes.MapGet("/{id}", async (AppDbContext db, string id) =>
        {
            var sw = Stopwatch.StartNew();
            var node = await db.Nodes.FindAsync(id);
            sw.Stop();
            if (node == null) return Results.NotFound(new ApiResponse<object>(false, null, $"Node '{id}' not found."));
            return Results.Ok(new ApiResponse<NodeEntity>(true, node, "Node details retrieved.", sw.ElapsedMilliseconds));
        }).WithSummary("Get cluster node by ID");

        nodes.MapPost("/{id}/heartbeat", async (AppDbContext db, string id, EventBroadcaster broadcaster) =>
        {
            var sw = Stopwatch.StartNew();
            var node = await db.Nodes.FindAsync(id);
            if (node == null) return Results.NotFound(new ApiResponse<object>(false, null, $"Node '{id}' not found."));

            node.LastHeartbeat = DateTime.UtcNow;
            node.Status = "Active";
            node.LoadAverage = Math.Round(new Random().NextDouble() * 0.5 + 0.1, 2);
            await db.SaveChangesAsync();

            await broadcaster.BroadcastAsync(JsonSerializer.Serialize(new { eventType = "node_heartbeat", node }), CancellationToken.None);

            sw.Stop();
            return Results.Ok(new ApiResponse<NodeEntity>(true, node, "Node heartbeat acknowledged.", sw.ElapsedMilliseconds));
        }).WithSummary("Send heartbeat for a node");

        nodes.MapDelete("/{id}", async (AppDbContext db, string id, EventBroadcaster broadcaster) =>
        {
            var sw = Stopwatch.StartNew();
            var node = await db.Nodes.FindAsync(id);
            if (node == null) return Results.NotFound(new ApiResponse<object>(false, null, $"Node '{id}' not found."));

            db.Nodes.Remove(node);
            await db.SaveChangesAsync();

            await broadcaster.BroadcastAsync(JsonSerializer.Serialize(new { eventType = "node_decommissioned", nodeId = id }), CancellationToken.None);

            sw.Stop();
            return Results.Ok(new ApiResponse<object>(true, new { id, status = "Decommissioned" }, "Node successfully removed from cluster.", sw.ElapsedMilliseconds));
        }).WithSummary("Decommission a cluster node");

        // Job & Task Queue Endpoints
        var jobs = api.MapGroup("/jobs").WithTags("Background Jobs");

        jobs.MapGet("/", async (AppDbContext db) =>
        {
            var sw = Stopwatch.StartNew();
            var list = await db.Jobs.OrderByDescending(j => j.EnqueuedAt).Take(50).ToListAsync();
            sw.Stop();
            return Results.Ok(new ApiResponse<List<JobTask>>(true, list, "Jobs list retrieved.", sw.ElapsedMilliseconds));
        }).WithSummary("List all background tasks");

        jobs.MapPost("/", async (AppDbContext db, [FromBody] CreateJobRequest req, IBackgroundJobQueue queue) =>
        {
            var sw = Stopwatch.StartNew();
            var job = new JobTask
            {
                Title = string.IsNullOrWhiteSpace(req.Title) ? $"{req.JobType} Routine #{new Random().Next(100, 999)}" : req.Title,
                JobType = req.JobType,
                EstimatedDurationSec = req.EstimatedDurationSec > 0 ? req.EstimatedDurationSec : 5,
                Status = "Queued",
                ProgressPercent = 0
            };

            db.Jobs.Add(job);
            await db.SaveChangesAsync();

            await queue.EnqueueAsync(job);

            sw.Stop();
            return Results.Accepted($"/api/v1/jobs/{job.Id}", new ApiResponse<JobTask>(true, job, "Job enqueued into background channel processing pipeline.", sw.ElapsedMilliseconds));
        }).WithSummary("Enqueue a new background task");

        jobs.MapGet("/{id}", async (AppDbContext db, string id) =>
        {
            var sw = Stopwatch.StartNew();
            var job = await db.Jobs.FindAsync(id);
            sw.Stop();
            if (job == null) return Results.NotFound(new ApiResponse<object>(false, null, $"Job '{id}' not found."));
            return Results.Ok(new ApiResponse<JobTask>(true, job, "Job details retrieved.", sw.ElapsedMilliseconds));
        }).WithSummary("Get job progress and status");

        jobs.MapDelete("/{id}", async (AppDbContext db, string id) =>
        {
            var sw = Stopwatch.StartNew();
            var job = await db.Jobs.FindAsync(id);
            if (job == null) return Results.NotFound(new ApiResponse<object>(false, null, $"Job '{id}' not found."));

            db.Jobs.Remove(job);
            await db.SaveChangesAsync();

            sw.Stop();
            return Results.Ok(new ApiResponse<object>(true, new { id, status = "Deleted" }, "Job removed.", sw.ElapsedMilliseconds));
        }).WithSummary("Delete or cancel a job");
    }
}
