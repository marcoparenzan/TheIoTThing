using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace TheIoTThing.Abstractions;

/// <summary>
/// Base class for a service that runs by advancing itself on a fixed interval — the Stopped/Running/
/// Paused state machine, the pause gate, and the tick loop (with per-tick error isolation) are handled
/// here once; a subclass only implements what actually happens on a tick. Flow's Executive and the
/// counter service both derive from this (see the docs for "how to build an orchestrable service").
/// </summary>
public abstract class TickingService(ILogger? logger = null) : IOrchestratedService
{
    readonly ILogger logger = logger ?? NullLogger.Instance;
    readonly object gate = new();
    readonly SemaphoreSlim resumeSignal = new(0);

    CancellationTokenSource? cts;
    Task? loopTask;

    public ServiceStatus Status { get; private set; } = ServiceStatus.Stopped;

    public bool Autostart { get; protected set; }

    protected abstract TimeSpan TickInterval { get; }

    /// <summary>Whether a call to StartAsync from Stopped has something to run (i.e. LoadAsync happened).</summary>
    protected abstract bool IsLoaded { get; }

    /// <summary>Advance this service by one tick. Exceptions are caught, logged, and don't stop the loop.</summary>
    protected abstract Task OnTickAsync();

    public abstract Task LoadAsync(string path, OrchestrationContext context);

    public abstract object? GetSnapshot();

    // In-memory only — persisting the flag to the service's own config file is the host's job.
    public void SetAutostart(bool value)
    {
        lock (gate)
        {
            Autostart = value;
        }
    }

    public Task StartAsync()
    {
        lock (gate)
        {
            if (Status == ServiceStatus.Running) return Task.CompletedTask;

            if (Status == ServiceStatus.Paused)
            {
                Status = ServiceStatus.Running;
                resumeSignal.Release();
                return Task.CompletedTask;
            }

            if (!IsLoaded)
            {
                throw new InvalidOperationException("No config has been loaded. Call LoadAsync first.");
            }

            cts = new CancellationTokenSource();
            Status = ServiceStatus.Running;
            loopTask = Task.Run(() => RunLoopAsync(cts.Token));
            return Task.CompletedTask;
        }
    }

    public Task PauseAsync()
    {
        lock (gate)
        {
            if (Status == ServiceStatus.Running)
            {
                Status = ServiceStatus.Paused;
            }
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? currentCts;
        Task? currentLoopTask;
        lock (gate)
        {
            currentCts = cts;
            currentLoopTask = loopTask;
            Status = ServiceStatus.Stopped;
        }

        if (currentCts is null) return;

        currentCts.Cancel();

        if (currentLoopTask is not null)
        {
            try
            {
                await currentLoopTask;
            }
            catch (OperationCanceledException) { }
        }

        lock (gate)
        {
            cts = null;
            loopTask = null;
        }
    }

    async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var periodicTimer = new PeriodicTimer(TickInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            if (Status == ServiceStatus.Paused)
            {
                await resumeSignal.WaitAsync(cancellationToken);
                continue;
            }

            await periodicTimer.WaitForNextTickAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested) break;
            if (Status == ServiceStatus.Paused) continue;

            try
            {
                await OnTickAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while advancing the service");
            }
        }
    }
}
