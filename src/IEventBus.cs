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
    /// <returns>A disposable that removes the subscription when disposed.</returns>
    IDisposable Subscribe<T>(Func<T, CancellationToken, Task> handler);
}
