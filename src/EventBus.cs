using System.Collections.Concurrent;

namespace Philiprehberger.EventBus;

/// <summary>
/// In-process publish/subscribe event bus that dispatches events to registered handlers.
/// </summary>
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers = new();
    private readonly EventBusOptions _options;
    private readonly List<Func<EventContext, Func<Task>, Task>> _middleware = new();
    private readonly object _middlewareLock = new();

    private readonly object _historyLock = new();
    private object[]? _historyBuffer;
    private Func<object, CancellationToken, Task>[]? _historyPublishers;
    private int _historyHead;
    private int _historyCount;

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

        RecordHistory(@event);

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

    /// <inheritdoc />
    public void Use(Func<EventContext, Func<Task>, Task> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        lock (_middlewareLock)
        {
            _middleware.Add(middleware);
        }
    }

    /// <inheritdoc />
    public void EnableHistory(int maxEvents)
    {
        if (maxEvents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEvents), "Max events must be greater than zero.");
        }

        lock (_historyLock)
        {
            _historyBuffer = new object[maxEvents];
            _historyPublishers = new Func<object, CancellationToken, Task>[maxEvents];
            _historyHead = 0;
            _historyCount = 0;
        }
    }

    /// <inheritdoc />
    public bool IsHistoryEnabled
    {
        get
        {
            lock (_historyLock)
            {
                return _historyBuffer is not null;
            }
        }
    }

    /// <inheritdoc />
    public void DisableHistory()
    {
        lock (_historyLock)
        {
            _historyBuffer = null;
            _historyPublishers = null;
            _historyHead = 0;
            _historyCount = 0;
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe<T>(IEventHandler<T> handler, int priority = 0, Func<T, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Subscribe<T>(handler.HandleAsync, priority, filter);
    }

    /// <inheritdoc />
    public async Task ReplayLastAsync(int count, CancellationToken ct = default)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must not be negative.");
        }

        (object Event, Func<object, CancellationToken, Task> Publisher)[] toReplay;

        lock (_historyLock)
        {
            if (_historyBuffer is null || _historyPublishers is null)
            {
                throw new InvalidOperationException("Event history is not enabled. Call EnableHistory first.");
            }

            var replayCount = Math.Min(count, _historyCount);
            toReplay = new (object, Func<object, CancellationToken, Task>)[replayCount];

            var startIndex = (_historyHead - replayCount + _historyBuffer.Length) % _historyBuffer.Length;
            for (var i = 0; i < replayCount; i++)
            {
                var idx = (startIndex + i) % _historyBuffer.Length;
                toReplay[i] = (_historyBuffer[idx], _historyPublishers[idx]);
            }
        }

        foreach (var (evt, publisher) in toReplay)
        {
            ct.ThrowIfCancellationRequested();
            await publisher(evt, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        lock (_historyLock)
        {
            if (_historyBuffer is null)
            {
                throw new InvalidOperationException("Event history is not enabled. Call EnableHistory first.");
            }

            Array.Clear(_historyBuffer);
            Array.Clear(_historyPublishers!);
            _historyHead = 0;
            _historyCount = 0;
        }
    }

    /// <inheritdoc />
    public bool HasSubscribers<T>()
    {
        if (!_handlers.TryGetValue(typeof(T), out var handlerList))
        {
            return false;
        }

        lock (handlerList)
        {
            return handlerList.Count > 0;
        }
    }

    /// <inheritdoc />
    public IDisposable SubscribeOnce<T>(Func<T, CancellationToken, Task> handler, Func<T, bool>? filter = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        IDisposable? subscription = null;
        var invoked = 0;

        subscription = Subscribe<T>(async (e, ct) =>
        {
            if (Interlocked.CompareExchange(ref invoked, 1, 0) == 0)
            {
                try
                {
                    await handler(e, ct).ConfigureAwait(false);
                }
                finally
                {
                    subscription?.Dispose();
                }
            }
        }, filter: filter);

        if (invoked == 1)
        {
            subscription.Dispose();
        }

        return subscription;
    }

    /// <inheritdoc />
    public int GetSubscriberCount<T>()
    {
        if (!_handlers.TryGetValue(typeof(T), out var handlerList))
        {
            return 0;
        }

        lock (handlerList)
        {
            return handlerList.Count;
        }
    }

    /// <inheritdoc />
    public void UnsubscribeAll<T>()
    {
        if (_handlers.TryGetValue(typeof(T), out var handlerList))
        {
            lock (handlerList)
            {
                handlerList.Clear();
            }
        }
    }

    /// <inheritdoc />
    public void UnsubscribeAll()
    {
        foreach (var kvp in _handlers)
        {
            lock (kvp.Value)
            {
                kvp.Value.Clear();
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<object> GetHistory()
    {
        lock (_historyLock)
        {
            if (_historyBuffer is null)
            {
                throw new InvalidOperationException("Event history is not enabled. Call EnableHistory first.");
            }

            var result = new object[_historyCount];
            var startIndex = (_historyHead - _historyCount + _historyBuffer.Length) % _historyBuffer.Length;
            for (var i = 0; i < _historyCount; i++)
            {
                result[i] = _historyBuffer[(startIndex + i) % _historyBuffer.Length];
            }

            return result;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<T> GetHistory<T>()
    {
        lock (_historyLock)
        {
            if (_historyBuffer is null)
            {
                throw new InvalidOperationException("Event history is not enabled. Call EnableHistory first.");
            }

            var result = new List<T>(_historyCount);
            var startIndex = (_historyHead - _historyCount + _historyBuffer.Length) % _historyBuffer.Length;
            for (var i = 0; i < _historyCount; i++)
            {
                var evt = _historyBuffer[(startIndex + i) % _historyBuffer.Length];
                if (evt is T typed)
                {
                    result.Add(typed);
                }
            }

            return result;
        }
    }

    /// <inheritdoc />
    public Task<T> WaitForAsync<T>(Func<T, bool>? filter = null, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        var subscription = SubscribeOnce<T>((e, _) =>
        {
            tcs.TrySetResult(e);
            return Task.CompletedTask;
        }, filter: filter);

        ct.Register(() =>
        {
            subscription.Dispose();
            tcs.TrySetCanceled(ct);
        });

        return tcs.Task;
    }

    private void RecordHistory<T>(T @event)
    {
        lock (_historyLock)
        {
            if (_historyBuffer is null || _historyPublishers is null)
            {
                return;
            }

            _historyBuffer[_historyHead] = @event!;
            _historyPublishers[_historyHead] = (obj, token) => PublishAsync((T)obj, token);
            _historyHead = (_historyHead + 1) % _historyBuffer.Length;
            if (_historyCount < _historyBuffer.Length)
            {
                _historyCount++;
            }
        }
    }

    private async Task InvokeHandler<T>(HandlerRegistration<T> registration, T @event, CancellationToken ct)
    {
        try
        {
            if (registration.Filter is not null && !registration.Filter(@event))
            {
                return;
            }

            Func<EventContext, Func<Task>, Task>[] middlewareSnapshot;
            lock (_middlewareLock)
            {
                middlewareSnapshot = _middleware.ToArray();
            }

            async Task CoreHandler()
            {
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

            if (middlewareSnapshot.Length > 0)
            {
                var context = new EventContext(@event!, typeof(T), ct);
                await BuildPipeline(middlewareSnapshot, 0, context, CoreHandler)().ConfigureAwait(false);
            }
            else
            {
                await CoreHandler().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _options.OnHandlerError?.Invoke(ex);

            if (!_options.ThrowOnHandlerError)
            {
                _options.OnDeadLetter?.Invoke(@event!, ex);
            }

            if (_options.ThrowOnHandlerError)
            {
                throw;
            }
        }
    }

    private static Func<Task> BuildPipeline(
        Func<EventContext, Func<Task>, Task>[] middleware,
        int index,
        EventContext context,
        Func<Task> core)
    {
        if (index >= middleware.Length)
        {
            return core;
        }

        return () => middleware[index](context, BuildPipeline(middleware, index + 1, context, core));
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
