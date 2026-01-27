# Runbook (Incidents)

This runbook is a practical checklist for diagnosing and mitigating issues in `Spx.DeltaWorker`.

## Quick facts

- Project: `worker/Spx.DeltaWorker`
- Config section: `Delta` (see [configuration](configuration.md))
- Safe default: `Delta:Enabled=false`

## Incident checklist (first 5 minutes)

1. Identify scope
   - Is the worker running at all?
   - Is it processing ticks? Is it crashing/restarting?
   - Is the issue isolated to one machine/environment?

2. Collect basic info
   - Current commit/version deployed
   - Current config values (at least `Delta:Enabled`, `Delta:PollIntervalSeconds`)
   - Approximate start time of the incident and any recent changes

3. Grab logs
   - Look for:
     - `Spx.DeltaWorker starting.` / `Spx.DeltaWorker stopping.`
     - `Unhandled exception during delta processing tick.`
     - `Delta processing is disabled (Delta:Enabled=false). Skipping tick.`

4. Stabilize
   - If processing is causing downstream harm, disable safely:
     - set `Delta:Enabled=false` and restart

## Common symptoms

### The worker is running but doing nothing

Expected if `Delta:Enabled=false`.

Actions:
- Confirm `Delta:Enabled`.
- Confirm logs contain either debug line (disabled) or info line (tick executed).

### The worker keeps crashing/restarting

Actions:
- Search for the first exception in logs.
- Confirm configuration is valid (options validation can fail startup).
- Temporarily set `Delta:Enabled=false` to prevent repeated tick errors while investigating.

### High CPU / tight loop

Actions:
- Confirm `Delta:PollIntervalSeconds` is not too low.
- Check for repeated exceptions (they trigger a 5s backoff).

## Diagnostics commands (developer machine)

From repo root:

```bash
dotnet --info
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release -warnaserror
dotnet test SPX2.slnx -c Release
```

## Post-incident

- Create an issue describing:
  - impact
  - timeline
  - root cause
  - mitigation
  - follow-up actions
- Add tests for regressions where possible.
