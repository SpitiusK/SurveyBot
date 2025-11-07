using System.Threading.Channels;

namespace SurveyBot.API.Services;

/// <summary>
/// Thread-safe queue for background task processing.
/// Uses System.Threading.Channels for high-performance async operations.
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly ILogger<BackgroundTaskQueue> _logger;

    /// <summary>
    /// Initializes a new instance of BackgroundTaskQueue.
    /// </summary>
    /// <param name="capacity">Maximum queue capacity. Default is 100.</param>
    /// <param name="logger">Logger instance.</param>
    public BackgroundTaskQueue(int capacity, ILogger<BackgroundTaskQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create bounded channel with specified capacity
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(options);

        _logger.LogInformation("BackgroundTaskQueue initialized with capacity {Capacity}", capacity);
    }

    /// <summary>
    /// Queues a background work item for processing.
    /// </summary>
    /// <param name="workItem">The work item to execute.</param>
    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        if (!_queue.Writer.TryWrite(workItem))
        {
            _logger.LogWarning("Failed to queue background work item - queue may be full");
        }
        else
        {
            _logger.LogDebug("Background work item queued successfully");
        }
    }

    /// <summary>
    /// Dequeues a background work item for processing.
    /// Blocks asynchronously until a work item is available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work item to execute.</returns>
    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        _logger.LogDebug("Background work item dequeued");

        return workItem;
    }
}
