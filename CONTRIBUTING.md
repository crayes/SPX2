# Contributing

## Ground rules

- Production code lives under `worker/`.
- Legacy material lives under `tools/` and should not be wired into builds.

## Required checks (same as CI)

From repo root:

```bash
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release -warnaserror
dotnet test SPX2.slnx -c Release

dotnet format SPX2.slnx --verify-no-changes
```

## Style

- Prefer small, reviewable PRs.
- Keep `DeltaEngine` and `Worker` easy to reason about; split into services if logic grows.
