namespace Philiprehberger.EventBus;

/// <summary>
/// An event that a handler failed to process, captured by the in-memory dead-letter store
/// when <see cref="EventBusOptions.MaxDeadLetters"/> is greater than zero.
/// </summary>
/// <param name="Event">The event instance that the handler failed to process.</param>
/// <param name="Exception">The exception thrown by the handler.</param>
/// <param name="Timestamp">The time at which the failure was recorded.</param>
public sealed record DeadLetter(object Event, Exception Exception, DateTimeOffset Timestamp);
