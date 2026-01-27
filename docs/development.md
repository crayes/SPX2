# Development

## Prerequisites

- .NET SDK as specified in `global.json`

## Common commands

From the repo root:

```bash
dotnet --info

dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release -warnaserror

dotnet test SPX2.slnx -c Release

# Formatting (CI enforces this)
dotnet format SPX2.slnx --verify-no-changes
```

## Solution format

This repo uses `SPX2.slnx` (solution XML format). Use the solution file for restore/build/test/format.

## Running locally

```bash
dotnet run --project worker/Spx.DeltaWorker
```

## Troubleshooting

- **SDK mismatch**: install the version specified in `global.json`.
- **Format failures**: run `dotnet format SPX2.slnx` locally and commit the changes.
- **Options validation**: `Delta:PollIntervalSeconds` must be between 1 and 3600.
