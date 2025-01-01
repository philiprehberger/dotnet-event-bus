namespace Philiprehberger.EventBus;

/// <summary>
/// Defines a handler for events of a specific type.
/// </summary>
/// <typeparam name="T">The type of the event to handle.</typeparam>
public interface IEventHandler<in T>
{
    /// <summary>
    /// Handles the specified event asynchronously.
    /// </summary>
    /// <param name="event">The event instance to handle.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(T @event, CancellationToken ct);
}
