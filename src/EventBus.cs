using System.Collections.Concurrent;

namespace Philiprehberger.EventBus;

/// <summary>
/// In-process publish/subscribe event bus that dispatches events to registered handlers.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly EventBusOptions _options;

    /// <summary>
    /// Creates a new <see cref="EventBus"/> instance with default options.
    /// </summary>
    public EventBus() : this(new EventBusOptions())
    {
    }

    /// <summary>
    /// Creates a new <see cref="EventBus"/> instance with the specified options.
    /// </summary>
    /// <param name="options">Configuration options for the event bus.</param>
    public EventBus(EventBusOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public async Task PublishAsync<T>(T @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(T);

        if (!_handlers.TryGetValue(eventType, out var handlerList))
        {
            return;
        }

        Func<T, CancellationToken, Task>[] snapshot;
        lock (handlerList)
        {
            snapshot = handlerList.Cast<Func<T, CancellationToken, Task>>().ToArray();
        }

        if (snapshot.Length == 0)
        {
            return;
        }

        if (_options.MaxConcurrency > 0)
        {
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var tasks = snapshot.Select(async handler =>
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await InvokeHandler(handler, @event, ct).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        else
        {
            var tasks = snapshot.Select(handler => InvokeHandler(handler, @event, ct));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(T);
        var handlerList = _handlers.GetOrAdd(eventType, _ => new List<object>());

        lock (handlerList)
        {
            handlerList.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (handlerList)
            {
                handlerList.Remove(handler);
            }
        });
    }

    private async Task InvokeHandler<T>(Func<T, CancellationToken, Task> handler, T @event, CancellationToken ct)
    {
        try
        {
            await handler(@event, ct).ConfigureAwait(false);
        }
        catch when (!_options.ThrowOnHandlerError)
        {
            // Swallow exception when ThrowOnHandlerError is false
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _unsubscribe;

        public Subscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        }
    }
}
