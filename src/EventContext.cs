namespace Philiprehberger.EventBus;

/// <summary>
/// Provides contextual information about the event being processed through the middleware pipeline.
/// </summary>
public sealed class EventContext
{
    /// <summary>
    /// Gets the event instance being published.
    /// </summary>
    public object Event { get; }

    /// <summary>
    /// Gets the CLR type of the event.
    /// </summary>
    public Type EventType { get; }

    /// <summary>
    /// Gets the cancellation token for the current publish operation.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets a dictionary that middleware can use to pass data along the pipeline.
    /// </summary>
    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Creates a new <see cref="EventContext"/> instance.
    /// </summary>
    /// <param name="event">The event instance.</param>
    /// <param name="eventType">The CLR type of the event.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    public EventContext(object @event, Type eventType, CancellationToken cancellationToken)
    {
        Event = @event;
        EventType = eventType;
        CancellationToken = cancellationToken;
    }
}
