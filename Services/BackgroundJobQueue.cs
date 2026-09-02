using System.Threading.Channels;
using NetPulseCore.Models;

namespace NetPulseCore.Services;

public interface IBackgroundJobQueue
{
    ValueTask EnqueueAsync(JobTask job, CancellationToken cancellationToken = default);
    ValueTask<JobTask> DequeueAsync(CancellationToken cancellationToken = default);
}

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<JobTask> _channel;

    public BackgroundJobQueue(int capacity = 500)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<JobTask>(options);
    }

    public ValueTask EnqueueAsync(JobTask job, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<JobTask> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
