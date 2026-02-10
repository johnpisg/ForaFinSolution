using System.Threading.Channels;

namespace ForaFinApi.Utils;
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<BgWorkItem> _queue;

    public BackgroundTaskQueue(int capacity)
    {
        // Limitamos la capacidad para no agotar la RAM si hay demasiadas peticiones
        var options = new BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.Wait };
        _queue = Channel.CreateBounded<BgWorkItem>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(BgWorkItem workItem) =>
        await _queue.Writer.WriteAsync(workItem);

    public bool TryQueueBackgroundWorkItem(BgWorkItem workItem) =>
        _queue.Writer.TryWrite(workItem);

    public async ValueTask<BgWorkItem> DequeueAsync(CancellationToken cancellationToken) =>
        await _queue.Reader.ReadAsync(cancellationToken);
}