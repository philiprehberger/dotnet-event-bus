# Changelog

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
