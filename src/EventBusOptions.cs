namespace Philiprehberger.EventBus;

/// <summary>
/// Configuration options for the event bus.
/// </summary>
public sealed class EventBusOptions
{
    /// <summary>
    /// When <c>true</c>, exceptions thrown by handlers are propagated to the publisher.
    /// When <c>false</c> (default), handler exceptions are swallowed (after invoking <see cref="OnHandlerError"/> if set).
    /// </summary>
    public bool ThrowOnHandlerError { get; set; }

    /// <summary>
    /// Maximum number of handlers to invoke concurrently. A value of <c>0</c> (default) means unlimited.
    /// Ignored when <see cref="SequentialDispatch"/> is <c>true</c>.
    /// </summary>
    public int MaxConcurrency { get; set; }

    /// <summary>
    /// When <c>true</c>, handlers for a published event are awaited one at a time in ascending
    /// <c>priority</c> order, so each handler completes before the next begins. This makes the
    /// <c>priority</c> parameter fully deterministic and enables ordered pipelines.
    /// When <c>false</c> (default), all handlers are dispatched concurrently. Overrides
    /// <see cref="MaxConcurrency"/> when enabled.
    /// </summary>
    public bool SequentialDispatch { get; set; }

    /// <summary>
    /// Optional callback invoked when a handler throws an exception, enabling centralized error logging.
    /// Called regardless of <see cref="ThrowOnHandlerError"/>.
    /// </summary>
    public Action<Exception>? OnHandlerError { get; set; }

    /// <summary>
    /// Optional timeout applied to each handler invocation. When set, a <see cref="TimeoutException"/>
    /// is thrown if a handler does not complete within the specified duration.
    /// </summary>
    public TimeSpan? HandlerTimeout { get; set; }

    /// <summary>
    /// Optional dead-letter handler invoked when a handler throws and <see cref="ThrowOnHandlerError"/> is <c>false</c>.
    /// Receives the event that failed and the exception that was thrown.
    /// </summary>
    public Action<object, Exception>? OnDeadLetter { get; set; }
}
