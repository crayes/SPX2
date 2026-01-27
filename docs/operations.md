# Operations

## Running

Local run:

```bash
dotnet run --project worker/Spx.DeltaWorker -c Release
```

## Logging

Logging is standard .NET hosting logging. Configure levels via `appsettings.json` or environment variables.

Example:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Spx.DeltaWorker": "Debug"
    }
  }
}
```

## Safe defaults

- `Delta:Enabled` defaults to `false` to prevent accidental execution.

## Delta state and output

- Delta cursor/state is persisted to `SharePoint:DeltaStateFile` (default `.state/sharepoint-delta.json`).
- Extracted metadata is appended as NDJSON to `SharePoint:OutputNdjsonPath` (default `.out/sharepoint-metadata.ndjson`).

## Health/monitoring

Today the worker only emits logs and `ActivitySource` traces. If you need readiness/liveness endpoints, add a lightweight HTTP health endpoint in a future change (e.g., `Microsoft.Extensions.Diagnostics.HealthChecks`).
