namespace Philiprehberger.EventBus;

/// <summary>
/// Defines a lightweight in-process publish/subscribe event bus.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all registered handlers for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of the event.</typeparam>
    /// <param name="event">The event instance to publish.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that completes when all handlers have been invoked.</returns>
    Task PublishAsync<T>(T @event, CancellationToken ct = default);

    /// <summary>
    /// Subscribes a handler function to events of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler function to invoke when an event is published.</param>
    /// <param name="priority">Execution priority. Lower values execute first. Default is <c>0</c>.</param>
    /// <param name="filter">Optional predicate evaluated before invoking the handler. If it returns <c>false</c>, the handler is skipped.</param>
    /// <returns>A disposable that removes the subscription when disposed.</returns>
    IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler, int priority = 0, Func<T, bool>? filter = null);

    /// <summary>
    /// Registers a middleware function that wraps every handler invocation.
    /// Middleware functions are invoked in registration order. Each middleware receives an
    /// <see cref="EventContext"/> and a <c>next</c> delegate to invoke the remainder of the pipeline.
    /// </summary>
    /// <param name="middleware">The middleware function to register.</param>
    void Use(Func<EventContext, Func<Task>, Task> middleware);

    /// <summary>
    /// Enables event history tracking with a fixed-capacity circular buffer.
    /// </summary>
    /// <param name="maxEvents">The maximum number of events to retain. Must be greater than zero.</param>
    void EnableHistory(int maxEvents);

    /// <summary>
    /// Re-publishes the most recent <paramref name="count"/> events from the history buffer.
    /// Events are replayed in the order they were originally published.
    /// Requires <see cref="EnableHistory"/> to have been called first.
    /// </summary>
    /// <param name="count">The number of recent events to replay.</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>A task that completes when all replayed events have been processed.</returns>
    Task ReplayLastAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Clears all events from the history buffer without disabling history tracking.
    /// Requires <see cref="EnableHistory"/> to have been called first.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Returns <c>true</c> if there are any handlers registered for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns><c>true</c> if at least one handler is registered; otherwise <c>false</c>.</returns>
    bool HasSubscribers<T>();

    /// <summary>
    /// Subscribes a handler that is automatically unsubscribed after it handles one event.
    /// </summary>
    /// <typeparam name="T">The type of the event to subscribe to.</typeparam>
    /// <param name="handler">The handler function to invoke once.</param>
    /// <param name="filter">Optional predicate evaluated before invoking the handler. If it returns <c>false</c>, the handler is skipped and remains subscribed.</param>
    /// <returns>A disposable that can cancel the subscription before the event arrives.</returns>
    IDisposable SubscribeOnce<T>(Func<T, CancellationToken, Task> handler, Func<T, bool>? filter = null);

    /// <summary>
    /// Returns a task that completes with the next event of type <typeparamref name="T"/> that matches the optional filter.
    /// </summary>
    /// <typeparam name="T">The type of the event to wait for.</typeparam>
    /// <param name="filter">Optional predicate; non-matching events are ignored.</param>
    /// <param name="ct">Cancellation token that cancels the wait.</param>
    /// <returns>A task that completes with the next matching event.</returns>
    Task<T> WaitForAsync<T>(Func<T, bool>? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of handlers registered for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type to check.</typeparam>
    /// <returns>The number of registered handlers, or <c>0</c> if none are registered.</returns>
    int GetSubscriberCount<T>();

    /// <summary>
    /// Removes all handlers registered for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type whose handlers should be removed.</typeparam>
    void UnsubscribeAll<T>();

    /// <summary>
    /// Removes all handlers for all event types.
    /// </summary>
    void UnsubscribeAll();

    /// <summary>
    /// Returns a read-only snapshot of the event history buffer in chronological order.
    /// Requires <see cref="EnableHistory"/> to have been called first.
    /// </summary>
    /// <returns>A read-only list of recorded events, oldest first.</returns>
    IReadOnlyList<object> GetHistory();
}
