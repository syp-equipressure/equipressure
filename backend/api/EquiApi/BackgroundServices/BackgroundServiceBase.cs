namespace WordClock.BackgroundServices;

/// <summary>
///     A base class for background services that run at a specific time or interval
/// </summary>
/// <param name="clock">Clock instance to use</param>
/// <param name="scopeFactory">Service scope factory</param>
/// <param name="logger">The logger</param>
/// <typeparam name="T">Concrete background service type</typeparam>
internal abstract class BackgroundServiceBase<T>(
    IClock clock,
    IServiceScopeFactory scopeFactory,
    ILogger<T> logger) : BackgroundService
{
    /// <summary>
    ///     Gets the system clock
    /// </summary>
    protected IClock Clock { get; } = clock;

    /// <summary>
    ///     Gets the logger
    /// </summary>
    protected ILogger<T> Logger { get; } = logger;

    /// <summary>
    ///     Gets the name of the background service
    /// </summary>
    protected abstract string ServiceName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstTimeToRun = GetFirstTimeToRun();
        var lastRunTime = firstTimeToRun;
        await WaitForNextRunAsync(firstTimeToRun, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await ExecuteAsync(scope, stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute background task");
            }

            // if execution took longer than the interval we would get an issue - ignored for simplicity for now
            // mitigation would be to pass the runtime and possibly skip the next run, log errors,...
            // timestamp with planned value to avoid drift
            var nextTimeToRun = GetNextTimeToRun(lastRunTime);

            // if the execution took longer than the interval, skip the missed slots
            var now = Clock.GetCurrentInstant();
            while (nextTimeToRun < now)
            {
                Logger.LogDebug("Skipping missed run of {ServiceName} planned at {@TimeToRun}", ServiceName,
                                nextTimeToRun);
                nextTimeToRun = GetNextTimeToRun(nextTimeToRun);
            }

            lastRunTime = nextTimeToRun;
            await WaitForNextRunAsync(nextTimeToRun, stoppingToken);
        }
    }

    /// <summary>
    ///     Execute the background task
    /// </summary>
    /// <param name="scope">Service scope</param>
    /// <param name="stoppingToken">Cancellation token</param>
    protected abstract ValueTask ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken);

    /// <summary>
    ///     Get the first time to run the background task
    /// </summary>
    /// <returns>First time to run</returns>
    protected abstract Instant GetFirstTimeToRun();

    /// <summary>
    ///     Gets the next time to run the background task.
    ///     To be used from the second run on.
    /// </summary>
    /// <param name="lastRunTime">Previous run time</param>
    /// <returns>Next time to run the background task</returns>
    protected abstract Instant GetNextTimeToRun(Instant lastRunTime);

    private async ValueTask WaitForNextRunAsync(Instant timeToRun, CancellationToken stoppingToken)
    {
        var waitTime = timeToRun - Clock.GetCurrentInstant();
        if (waitTime < Duration.Zero)
        {
            Logger.LogError("Wait time cannot be negative, defaulting to 0");
            waitTime = Duration.Zero;
        }

        Logger.LogDebug("Scheduled next run of {ServiceName} at {@TimeToRun}", ServiceName, timeToRun);
        await Task.Delay(waitTime.ToTimeSpan(), stoppingToken);
    }
    
}
