# Changelog

## 0.5.0 (2026-04-13)

- Add `GetSubscriberCount<T>()` to return the number of handlers registered for an event type
- Add `UnsubscribeAll<T>()` to remove all handlers for a specific event type
- Add `UnsubscribeAll()` to remove all handlers for all event types
- Add `GetHistory()` to return a read-only snapshot of the event history buffer

## 0.4.0 (2026-04-11)

- Add `SubscribeOnce<T>` for one-time subscriptions that auto-unsubscribe after the first matching event
- Add `WaitForAsync<T>` to await the next event of a given type with optional filter and cancellation support
- Add `HasSubscribers<T>` to check whether any handlers are registered for an event type
- Add `ClearHistory` to reset the event history buffer without disabling tracking

## 0.3.0 (2026-03-31)

- Add dead-letter queue via `OnDeadLetter` option — routes failed events and exceptions to a configurable handler when errors are swallowed
- Add event replay with `EnableHistory(int maxEvents)` and `ReplayLastAsync(int count)` for circular buffer history tracking
- Add middleware pipeline via `Use(Func<EventContext, Func<Task>, Task>)` for cross-cutting concerns that wrap every handler invocation
- Add `EventContext` class exposing event metadata and an `Items` dictionary for middleware data passing

## 0.2.1 (2026-03-31)

- Standardize README to 3-badge format with emoji Support section
- Update CI actions to v5 for Node.js 24 compatibility

## 0.2.0 (2026-03-28)

- Add handler priority ordering with `priority` parameter on `Subscribe`
- Add handler filtering with `filter` predicate on `Subscribe`
- Add `OnHandlerError` callback to `EventBusOptions` for centralized error logging
- Add `HandlerTimeout` option with `TimeoutException` enforcement per handler invocation
- Add GitHub issue templates, dependabot configuration, and pull request template
- Add missing README badges (GitHub release, Last updated, Bug Reports, Feature Requests)
- Add Support section to README

## 0.1.3 (2026-03-26)

- Add Sponsor badge and fix License link format in README

## 0.1.2 (2026-03-24)

- Add unit tests
- Add test step to CI workflow

## 0.1.1 (2026-03-23)

- Shorten package description to meet 120-character limit

## 0.1.0 (2026-03-21)

- Initial release
- In-process publish/subscribe event bus
- Async handler support with cancellation
- Scoped subscriptions via disposable pattern
- Microsoft DI integration with handler scanning
- Configurable concurrency and error handling
