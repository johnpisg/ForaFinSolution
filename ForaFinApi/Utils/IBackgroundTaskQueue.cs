namespace ForaFinApi.Utils;

public record BgWorkItem(Guid TaskId, string StartsWith);
public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(BgWorkItem workItem);
    ValueTask<BgWorkItem> DequeueAsync(CancellationToken cancellationToken);
    bool TryQueueBackgroundWorkItem(BgWorkItem workItem);
}