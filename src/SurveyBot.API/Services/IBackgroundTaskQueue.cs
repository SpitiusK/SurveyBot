namespace SurveyBot.API.Services;

/// <summary>
/// Interface for queueing background tasks.
/// Provides thread-safe queue for executing work items asynchronously.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a background work item.
    /// </summary>
    /// <param name="workItem">The work item to execute.</param>
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);

    /// <summary>
    /// Dequeues a background work item for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work item to execute.</returns>
    Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
