using System.Diagnostics;
using System.Runtime.InteropServices;
using NetPulseCore.Models;
using NetPulseCore.Data;
using Microsoft.EntityFrameworkCore;

namespace NetPulseCore.Services;

public interface ITelemetryService
{
    Task<TelemetryMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}

public class TelemetryService : ITelemetryService
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private static DateTime _lastCpuCheck = DateTime.UtcNow;
    private static TimeSpan _lastTotalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;
    private static double _lastCpuUsage = 0.0;
    private static readonly object _cpuLock = new();

    public TelemetryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TelemetryMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var proc = Process.GetCurrentProcess();
        proc.Refresh();

        double cpuPercent = CalculateCpuUsage(proc);
        long workingSetMb = proc.WorkingSet64 / (1024 * 1024);
        long allocatedMb = GC.GetTotalMemory(false) / (1024 * 1024);

        ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);

        int activeNodes = 0;
        int pendingJobs = 0;
        int completedJobs = 0;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            activeNodes = await db.Nodes.CountAsync(n => n.Status == "Active", cancellationToken);
            pendingJobs = await db.Jobs.CountAsync(j => j.Status == "Queued" || j.Status == "Processing", cancellationToken);
            completedJobs = await db.Jobs.CountAsync(j => j.Status == "Completed", cancellationToken);
        }
        catch
        {
            // Initial boot fallback
        }

        var uptime = DateTime.UtcNow - _startTime;
        string uptimeFormatted = $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";

        return new TelemetryMetrics
        {
            ProcessCpuPercent = Math.Round(cpuPercent, 2),
            ProcessAllocatedMemoryMb = allocatedMb,
            WorkingSetMemoryMb = workingSetMb,
            GcTotalMemoryMb = allocatedMb,
            GcGen0Collections = GC.CollectionCount(0),
            GcGen1Collections = GC.CollectionCount(1),
            GcGen2Collections = GC.CollectionCount(2),
            ThreadCount = proc.Threads.Count,
            AvailableWorkerThreads = workerThreads,
            AvailableIoThreads = ioThreads,
            OsDescription = RuntimeInformation.OSDescription,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            MachineName = Environment.MachineName,
            ProcessUptime = uptimeFormatted,
            ActiveNodesCount = activeNodes,
            PendingJobsCount = pendingJobs,
            CompletedJobsCount = completedJobs,
            Timestamp = DateTime.UtcNow
        };
    }

    private static double CalculateCpuUsage(Process proc)
    {
        lock (_cpuLock)
        {
            var now = DateTime.UtcNow;
            var currentTotalTime = proc.TotalProcessorTime;
            var timePassed = (now - _lastCpuCheck).TotalMilliseconds;

            if (timePassed > 400)
            {
                var cpuPassed = (currentTotalTime - _lastTotalProcessorTime).TotalMilliseconds;
                _lastCpuCheck = now;
                _lastTotalProcessorTime = currentTotalTime;

                var totalCpu = (cpuPassed / (timePassed * Environment.ProcessorCount)) * 100.0;
                _lastCpuUsage = Math.Clamp(totalCpu, 0.0, 100.0);
            }

            return _lastCpuUsage;
        }
    }
}
