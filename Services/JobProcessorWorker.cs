using System.Text.Json;
using NetPulseCore.Data;
using NetPulseCore.Models;
using Microsoft.EntityFrameworkCore;

namespace NetPulseCore.Services;

public class JobProcessorWorker : BackgroundService
{
    private readonly IBackgroundJobQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventBroadcaster _broadcaster;
    private readonly ILogger<JobProcessorWorker> _logger;

    public JobProcessorWorker(
        IBackgroundJobQueue queue,
        IServiceProvider serviceProvider,
        EventBroadcaster broadcaster,
        ILogger<JobProcessorWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 NetPulse JobProcessorWorker started listening for background tasks.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queuedJob = await _queue.DequeueAsync(stoppingToken);
                await ProcessJobAsync(queuedJob, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing job in JobProcessorWorker.");
            }
        }

        _logger.LogInformation("🛑 NetPulse JobProcessorWorker is shutting down.");
    }

    private async Task ProcessJobAsync(JobTask job, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dbJob = await db.Jobs.FirstOrDefaultAsync(j => j.Id == job.Id, stoppingToken);
        if (dbJob == null) return;

        dbJob.Status = "Processing";
        dbJob.StartedAt = DateTime.UtcNow;
        dbJob.ProgressPercent = 10;
        await db.SaveChangesAsync(stoppingToken);

        await BroadcastJobEventAsync("job_started", dbJob, stoppingToken);

        int totalSteps = Math.Max(3, job.EstimatedDurationSec);
        int stepDelayMs = Math.Max(200, (job.EstimatedDurationSec * 1000) / totalSteps);

        for (int i = 1; i <= totalSteps; i++)
        {
            if (stoppingToken.IsCancellationRequested) break;

            await Task.Delay(stepDelayMs, stoppingToken);
            dbJob.ProgressPercent = (int)((double)i / totalSteps * 90.0);
            await db.SaveChangesAsync(stoppingToken);

            await BroadcastJobEventAsync("job_progress", dbJob, stoppingToken);
        }

        dbJob.ProgressPercent = 100;
        dbJob.Status = "Completed";
        dbJob.CompletedAt = DateTime.UtcNow;
        var durationSec = (dbJob.CompletedAt.Value - dbJob.StartedAt.Value).TotalSeconds;
        dbJob.Result = $"Success: Executed {dbJob.JobType} across cluster mesh in {durationSec:F1}s with zero faults.";

        await db.SaveChangesAsync(stoppingToken);
        await BroadcastJobEventAsync("job_completed", dbJob, stoppingToken);

        _logger.LogInformation("✅ Job {JobId} ({JobType}) completed successfully.", dbJob.Id, dbJob.JobType);
    }

    private async Task BroadcastJobEventAsync(string eventType, JobTask job, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            eventType = eventType,
            job = new
            {
                job.Id,
                job.Title,
                job.JobType,
                job.Status,
                job.ProgressPercent,
                job.Result,
                job.EnqueuedAt,
                job.StartedAt,
                job.CompletedAt
            },
            timestamp = DateTime.UtcNow
        });

        await _broadcaster.BroadcastAsync(payload, cancellationToken);
    }
}
