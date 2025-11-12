namespace SurveyBot.API.Services;

/// <summary>
/// Background service that processes queued background tasks.
/// Runs continuously and executes work items from the queue.
/// Creates a new service scope for each task to ensure proper dependency injection.
/// </summary>
public class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<QueuedHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of QueuedHostedService.
    /// </summary>
    public QueuedHostedService(
        IBackgroundTaskQueue taskQueue,
        IServiceProvider serviceProvider,
        ILogger<QueuedHostedService> logger)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the background service.
    /// Continuously dequeues and processes work items.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueuedHostedService is starting");

        await BackgroundProcessing(stoppingToken);
    }

    /// <summary>
    /// Background processing loop that executes queued tasks.
    /// </summary>
    private async Task BackgroundProcessing(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    _logger.LogDebug("Executing background work item with new service scope");

                    // Create a new service scope for each background task
                    // This ensures fresh instances of scoped services like DbContext
                    using var scope = _serviceProvider.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);

                    _logger.LogDebug("Background work item completed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background work item");
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation - service is stopping
                _logger.LogInformation("QueuedHostedService is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in background processing loop");

                // Delay before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("QueuedHostedService is stopping gracefully");

        await base.StopAsync(cancellationToken);
    }
}
