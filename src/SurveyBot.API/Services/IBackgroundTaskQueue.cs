namespace SurveyBot.API.Services;

/// <summary>
/// Interface for queueing background tasks.
/// Provides thread-safe queue for executing work items asynchronously.
/// Work items receive a service provider to create their own scoped services.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a background work item.
    /// </summary>
    /// <param name="workItem">The work item to execute. Receives a service provider and cancellation token.</param>
    void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem);

    /// <summary>
    /// Dequeues a background work item for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The work item to execute.</returns>
    Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
