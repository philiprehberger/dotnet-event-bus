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

        HandlerRegistration<T>[] snapshot;
        lock (handlerList)
        {
            snapshot = handlerList.Cast<HandlerRegistration<T>>().OrderBy(r => r.Priority).ToArray();
        }

        if (snapshot.Length == 0)
        {
            return;
        }

        if (_options.MaxConcurrency > 0)
        {
            using var semaphore = new SemaphoreSlim(_options.MaxConcurrency);
            var tasks = snapshot.Select(async registration =>
            {
                await semaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await InvokeHandler(registration, @event, ct).ConfigureAwait(false);
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
            var tasks = snapshot.Select(registration => InvokeHandler(registration, @event, ct));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler, int priority = 0, Func<T, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var registration = new HandlerRegistration<T>(handler, priority, filter);
        var eventType = typeof(T);
        var handlerList = _handlers.GetOrAdd(eventType, _ => new List<object>());

        lock (handlerList)
        {
            handlerList.Add(registration);
        }

        return new Subscription(() =>
        {
            lock (handlerList)
            {
                handlerList.Remove(registration);
            }
        });
    }

    private async Task InvokeHandler<T>(HandlerRegistration<T> registration, T @event, CancellationToken ct)
    {
        try
        {
            if (registration.Filter is not null && !registration.Filter(@event))
            {
                return;
            }

            if (_options.HandlerTimeout.HasValue)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(_options.HandlerTimeout.Value);

                try
                {
                    await registration.Handler(@event, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Handler for event type '{typeof(T).Name}' did not complete within the configured timeout of {_options.HandlerTimeout.Value.TotalMilliseconds}ms.");
                }
            }
            else
            {
                await registration.Handler(@event, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _options.OnHandlerError?.Invoke(ex);

            if (_options.ThrowOnHandlerError)
            {
                throw;
            }
        }
    }

    private sealed record HandlerRegistration<T>(
        Func<T, CancellationToken, Task> Handler,
        int Priority,
        Func<T, bool>? Filter);

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
