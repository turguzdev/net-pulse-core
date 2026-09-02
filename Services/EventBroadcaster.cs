using System.Collections.Concurrent;
using System.Threading.Channels;

namespace NetPulseCore.Services;

public class EventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<string>> _clients = new();

    public Guid Subscribe(out ChannelReader<string> reader)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleWriter = false,
            SingleReader = true
        });
        _clients[id] = channel;
        reader = channel.Reader;
        return id;
    }

    public void Unsubscribe(Guid id)
    {
        if (_clients.TryRemove(id, out var channel))
        {
            channel.Writer.TryComplete();
        }
    }

    public async Task BroadcastAsync(string message, CancellationToken cancellationToken = default)
    {
        foreach (var pair in _clients)
        {
            try
            {
                await pair.Value.Writer.WriteAsync(message, cancellationToken);
            }
            catch
            {
                _clients.TryRemove(pair.Key, out _);
            }
        }
    }
}
