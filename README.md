# SPX2

SPX2 is a .NET Worker Service repository. The **only production component** is the worker in `worker/Spx.DeltaWorker`.

> Note: The repository history/content was previously corrupted (many files were NUL bytes). The worker was rebuilt as a clean baseline. `DeltaEngine` is currently a safe **no-op placeholder** until the real business logic is recovered/migrated.

## Requirements

- .NET SDK pinned by `global.json` (recommended: install the exact version).

Check your SDK:

```bash
dotnet --info
```

## Quick start

Restore/build/test:

```bash
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release
dotnet test SPX2.slnx -c Release
```

Run the worker locally:

```bash
dotnet run --project worker/Spx.DeltaWorker
```

## Configuration

The worker reads configuration from the `Delta` section.

Example `appsettings.Development.json` override:

```json
{
  "Delta": {
    "Enabled": false,
    "PollIntervalSeconds": 5
  }
}
```

Environment variable overrides (standard .NET configuration):

- `Delta__Enabled=true`
- `Delta__PollIntervalSeconds=10`

See [docs/configuration.md](docs/configuration.md) for details.

## Repo layout

- `worker/` — production .NET worker (`Spx.DeltaWorker`)
- `tests/` — xUnit tests
- `tools/` — legacy/archived material (not part of production)

## CI

GitHub Actions runs on pushes/PRs to `main`:

- `dotnet restore/build/test`
- `dotnet format --verify-no-changes`

See [docs/development.md](docs/development.md).
