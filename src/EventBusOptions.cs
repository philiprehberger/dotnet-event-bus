namespace Philiprehberger.EventBus;

/// <summary>
/// Configuration options for the event bus.
/// </summary>
/// <param name="ThrowOnHandlerError">
/// When <c>true</c>, exceptions thrown by handlers are propagated to the publisher.
/// When <c>false</c> (default), handler exceptions are swallowed.
/// </param>
/// <param name="MaxConcurrency">
/// Maximum number of handlers to invoke concurrently. A value of <c>0</c> (default) means unlimited.
/// </param>
public record EventBusOptions(bool ThrowOnHandlerError = false, int MaxConcurrency = 0);
