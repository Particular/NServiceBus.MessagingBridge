# Engineering context

## About this repository

This repository bridges messaging between endpoints on different transports.

- `src/` — implementation and tests
- `.github/workflows/` — CI pipelines

## Start here

- [Component documentation](https://docs.particular.net/nservicebus/bridge/) — how bridge registrations and endpoint addresses are configured
- [Repository README](../README.md) — how to run the tests locally
- [Contributing](https://docs.particular.net/platform/contributing) — contribution process

## Decisions and rationale

- [Architecture and design decisions](decisions/)

### Decisions recorded in pull requests

- Use the registered custom queue address for both address translation and dispatch so forwarding and reply routing agree — [#925](https://github.com/Particular/NServiceBus.MessagingBridge/pull/925)

Keep this index current when a canonical source is added, replaced, or retired; link, do not copy.
