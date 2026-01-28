# Operations

## Running the Worker

### Local Development

```bash
# Normal run
dotnet run --project worker/Spx.DeltaWorker

# With debug logging
dotnet run --project worker/Spx.DeltaWorker -- --Logging:LogLevel:Default=Debug

# Release mode
dotnet run --project worker/Spx.DeltaWorker -c Release
```

### Production Deployment

```bash
# Build self-contained
dotnet publish worker/Spx.DeltaWorker -c Release -r linux-x64 --self-contained -o /opt/spx2

# Run
cd /opt/spx2
./Spx.DeltaWorker
```

## Safe Defaults

| Setting | Default | Purpose |
|---------|---------|---------|
| `Delta:Enabled` | `false` | Prevents accidental execution |
| `Delta:PollIntervalSeconds` | `5` | Fast for dev, increase in prod |
| `SharePoint:ForceUpdate` | `false` | Preserves manual edits |

## Logging Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information",
      "Spx.DeltaWorker": "Information",
      "Spx.DeltaWorker.Infrastructure": "Warning"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "TimestampFormat": "yyyy-MM-dd HH:mm:ss ",
        "SingleLine": true
      }
    }
  }
}
```

### Log Levels by Scenario

| Scenario | Recommended Level |
|----------|------------------|
| Production | `Information` |
| Troubleshooting | `Debug` for `Spx.DeltaWorker` |
| Initial Setup | `Debug` for all |
| Performance Testing | `Warning` (reduce log overhead) |

### Environment Variable Override

```bash
export Logging__LogLevel__Default=Debug
export Logging__LogLevel__Spx.DeltaWorker=Debug
```

## State and Output Files

| File | Purpose | Location |
|------|---------|----------|
| Delta state | Cursor for incremental sync | `SharePoint:DeltaStateFile` (default: `.state/sharepoint-delta.json`) |
| Metadata log | History of processed items | `SharePoint:OutputNdjsonPath` (default: `.out/sharepoint-metadata.ndjson`) |

### Delta State Structure

```json
{
  "continuationLink": null,
  "deltaLink": "https://graph.microsoft.com/v1.0/drives/.../delta?token=...",
  "siteId": "rfaasp.sharepoint.com,abc123...",
  "driveId": "b!xyz789...",
  "driveName": "Documentos",
  "savedAtUtc": "2026-01-28T14:30:00Z"
}
```

### State Management

- **First run**: No state file → processes all files
- **Subsequent runs**: Uses `deltaLink` → only changes
- **Interrupted run**: Uses `continuationLink` → resumes from last page
- **Reset sync**: Delete the state file

```bash
# Force full resync
rm .state/sharepoint-delta.json
```

## Monitoring

### Key Log Messages

| Message | Meaning |
|---------|---------|
| `Spx.DeltaWorker starting.` | Worker started successfully |
| `Starting delta processing with X parallel workers` | Tick started |
| `✅ Updated N fields: path` | File processed successfully |
| `⏭️ Skipped (fields already filled): name` | No update needed |
| `❌ Failed to update: path` | PATCH request failed |
| `⚠️ Rate limited (429). Reducing rate to N/s` | API throttling |
| `Delta tick complete: X processed, Y updated` | Tick completed |

### Metrics to Track

| Metric | Source | Alert Threshold |
|--------|--------|-----------------|
| Items processed per tick | Log: "Delta tick complete" | < expected if degraded |
| Update success rate | `updated / (updated + failed)` | < 95% |
| Processing time per tick | Tick start → complete | > 30 min for large batches |
| Rate limiter reductions | Log: "Reducing rate" | > 5 per hour |
| Worker restarts | Process monitoring | > 1 per day |

### Health Check Endpoint (Future)

Currently not implemented. Recommended approach:

```csharp
// Add to Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<DeltaEngineHealthCheck>("delta_engine");

app.MapHealthChecks("/health");
```

### Recommended Alerts

| Alert | Condition | Action |
|-------|-----------|--------|
| Worker not running | No logs for 2x `PollIntervalSeconds` | Check process, restart |
| High failure rate | > 10% failed per tick | Check Graph API status, credentials |
| Persistent throttling | > 10 rate reductions/hour | Reduce `MaxWorkers` and `RateLimitPerSecond` |
| State file missing | After expected run | Check disk space, permissions |

## Performance Tuning

### Recommended Settings by Library Size

| Library Size | MaxWorkers | RateLimitPerSecond | PollIntervalSeconds |
|--------------|------------|-------------------|---------------------|
| < 10,000 files | 10 | 10 | 300 (5 min) |
| 10,000 - 50,000 | 15 | 15 | 1800 (30 min) |
| 50,000 - 200,000 | 20 | 20 | 3600 (1 hour) |
| > 200,000 files | 20-30 | 20 | 7200 (2 hours) |

### First Run Expectations

| Library Size | Estimated Time | Notes |
|--------------|----------------|-------|
| 10,000 files | 10-20 minutes | With default settings |
| 50,000 files | 1-2 hours | Consider off-hours |
| 200,000 files | 4-8 hours | Run overnight |

### Incremental Runs

After first run, delta processing typically:
- Returns only changed files (usually < 1% of library)
- Completes in seconds to minutes
- Uses minimal API quota

## Backup and Recovery

### Backup State File

```bash
# Before maintenance
cp .state/sharepoint-delta.json .state/sharepoint-delta.json.bak

# Restore if needed
cp .state/sharepoint-delta.json.bak .state/sharepoint-delta.json
```

### Recovery Scenarios

| Scenario | Resolution |
|----------|------------|
| Corrupted state file | Delete file, let worker rebuild from delta |
| Wrong data written | Delete state, set `ForceUpdate=true` temporarily |
| Credential expired | Update secrets, restart worker |
| API unavailable | Worker auto-retries with backoff |

## Systemd Service (Linux)

```ini
# /etc/systemd/system/spx2.service
[Unit]
Description=SPX2 SharePoint Delta Worker
After=network.target

[Service]
Type=simple
User=spx2
WorkingDirectory=/opt/spx2
ExecStart=/opt/spx2/Spx.DeltaWorker
Restart=always
RestartSec=10
Environment=DOTNET_ENVIRONMENT=Production
Environment=Delta__Enabled=true
Environment=Delta__PollIntervalSeconds=7200

[Install]
WantedBy=multi-user.target
```

### Service Commands

```bash
# Start/stop
sudo systemctl start spx2
sudo systemctl stop spx2

# View logs
sudo journalctl -u spx2 -f

# Check status
sudo systemctl status spx2
```

## Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY out/publish .
ENTRYPOINT ["./Spx.DeltaWorker"]
```

```bash
# Build and run
docker build -t spx2 .
docker run -d \
  -e Delta__Enabled=true \
  -e Delta__PollIntervalSeconds=7200 \
  -e SharePoint__TenantId=xxx \
  -e SharePoint__ClientId=xxx \
  -e SharePoint__ClientSecret=xxx \
  -e SharePoint__SiteUrl=https://contoso.sharepoint.com/sites/docs \
  -v /data/spx2/state:/app/.state \
  -v /data/spx2/out:/app/.out \
  --name spx2 \
  spx2
```

## Troubleshooting Commands

```bash
# Check if worker is running
pgrep -f Spx.DeltaWorker

# View recent logs
tail -100 /var/log/spx2/worker.log

# Check state file
cat .state/sharepoint-delta.json | jq .

# Test Graph API connectivity
curl -I https://graph.microsoft.com/v1.0

# Verify NuGet sources
dotnet nuget list source

# Check .NET SDK version
dotnet --info
```
