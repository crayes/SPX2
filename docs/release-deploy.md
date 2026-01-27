# Release & Deploy

This guide documents a safe, repeatable process to release and deploy `Spx.DeltaWorker`.

> Deployment specifics depend on your runtime (service manager, container platform, VM). This document focuses on build artifacts and operational safety.

## Goals

- Reproducible build from `main`
- CI green before deploy
- Safe rollout with ability to disable processing (`Delta:Enabled=false`)

## Pre-release checklist

- `main` is up to date
- CI checks passing
- Local verification:

```bash
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release -warnaserror
dotnet test SPX2.slnx -c Release

dotnet format SPX2.slnx --verify-no-changes
```

## Build artifact

Recommended: publish self-contained or framework-dependent depending on your environment.

Framework-dependent publish (smaller, requires .NET runtime installed):

```bash
dotnet publish worker/Spx.DeltaWorker -c Release -o out/publish
```

Self-contained publish (bigger, no runtime required; choose the correct RID):

```bash
# Example for Linux x64
dotnet publish worker/Spx.DeltaWorker -c Release -r linux-x64 --self-contained true -o out/publish
```

## Deployment safety

- Start with `Delta:Enabled=false` on the new deployment.
- Verify the process starts and logs `Spx.DeltaWorker starting.`.
- Then enable processing:
  - set `Delta:Enabled=true`
  - restart (or reload config if your hosting supports it)

## Rollback plan

- Keep the previous publish directory / package.
- Roll back to the previous known-good version.
- If immediate rollback is not possible, set `Delta:Enabled=false` to stop processing safely.

## Versioning (recommended)

If you want stricter traceability, adopt one of:

- Git tags (`vX.Y.Z`) per release
- A `VERSION` file and CI stamping
- Embedding version in logs at startup (future improvement)
