# Architecture

## High-level

`Spx.DeltaWorker` is a .NET Worker Service:

- `Program.cs` bootstraps the host
- `ServiceCollectionExtensions.AddDeltaWorker` registers configuration + dependencies
- `Worker` is the hosted background service loop
- `IDeltaEngine` is the application entry point executed on each tick

Current implementation of `IDeltaEngine` is `SharePointDeltaEngine`, which calls Microsoft Graph delta endpoints to fetch only changed/new files and then extracts document metadata (ListItem fields).

## Execution loop

The worker:

- Creates a `PeriodicTimer` from `Delta:PollIntervalSeconds`
- On each tick:
  - creates a logging scope with a `tickTime` value
  - starts an Activity (`delta.tick`) via `DeltaActivity`
  - calls `IDeltaEngine.RunOnceAsync`
- On unhandled exceptions:
  - logs the exception
  - waits a short backoff (5 seconds)

## Business logic placeholder

`DeltaEngine` currently implements a no-op placeholder because the original repository state did not contain recoverable business logic.

When the real logic is ready, implement it inside `DeltaEngine` (or split into additional services under `Application/` and inject them via DI).

## Observability

- Logs: `ILogger` + `BeginScope` per tick
- Tracing: `System.Diagnostics.ActivitySource` (`Spx.DeltaWorker`) emitting `delta.tick`
