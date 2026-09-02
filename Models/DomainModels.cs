using System;

namespace NetPulseCore.Models;

public record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message = null,
    long ExecutionDurationMs = 0
)
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public class NodeEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string NodeName { get; set; } = string.Empty;
    public string Hostname { get; set; } = Environment.MachineName;
    public string Region { get; set; } = "eu-central-1";
    public string Status { get; set; } = "Active"; // Active, Degraded, Standby, Offline
    public int CpuCores { get; set; } = Environment.ProcessorCount;
    public long TotalMemoryMb { get; set; } = 16384;
    public string WorkloadType { get; set; } = "Compute"; // Compute, Storage, Ingress, AI-Worker
    public double LoadAverage { get; set; } = 0.12;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class JobTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..10];
    public string Title { get; set; } = string.Empty;
    public string JobType { get; set; } = "DataSync"; // DataSync, ModelInference, TelemetryAudit, CacheCompaction, StressBenchmark
    public string Status { get; set; } = "Queued"; // Queued, Processing, Completed, Failed, Cancelled
    public int ProgressPercent { get; set; } = 0;
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public int EstimatedDurationSec { get; set; } = 5;
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TelemetryMetrics
{
    public double ProcessCpuPercent { get; set; }
    public long ProcessAllocatedMemoryMb { get; set; }
    public long WorkingSetMemoryMb { get; set; }
    public long GcTotalMemoryMb { get; set; }
    public int GcGen0Collections { get; set; }
    public int GcGen1Collections { get; set; }
    public int GcGen2Collections { get; set; }
    public int ThreadCount { get; set; }
    public int AvailableWorkerThreads { get; set; }
    public int AvailableIoThreads { get; set; }
    public string OsDescription { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string ProcessUptime { get; set; } = string.Empty;
    public int ActiveNodesCount { get; set; }
    public int PendingJobsCount { get; set; }
    public int CompletedJobsCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CreateNodeRequest
{
    public string NodeName { get; set; } = string.Empty;
    public string Region { get; set; } = "eu-central-1";
    public string WorkloadType { get; set; } = "Compute";
    public int CpuCores { get; set; } = Environment.ProcessorCount;
    public long TotalMemoryMb { get; set; } = 16384;
}

public class CreateJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string JobType { get; set; } = "DataSync";
    public int EstimatedDurationSec { get; set; } = 5;
}

public class ClusterOverviewSummary
{
    public string EngineName { get; set; } = "NetPulse Core v1.0.0 (.NET 8)";
    public string Status { get; set; } = "Operational";
    public int TotalNodes { get; set; }
    public int HealthyNodes { get; set; }
    public int TotalJobsProcessed { get; set; }
    public int ActiveJobsRunning { get; set; }
    public double SystemCpuPercent { get; set; }
    public long WorkingSetMemoryMb { get; set; }
    public string ProcessUptime { get; set; } = string.Empty;
    public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
}
