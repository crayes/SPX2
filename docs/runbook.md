# Runbook (Incidents)

This runbook provides practical checklists for diagnosing and mitigating issues in `Spx.DeltaWorker`.

## Quick Reference

| Item | Value |
|------|-------|
| Project | `worker/Spx.DeltaWorker` |
| Config section | `Delta` and `SharePoint` |
| Safe default | `Delta:Enabled=false` |
| State file | `.state/sharepoint-delta.json` |
| Output file | `.out/sharepoint-metadata.ndjson` |

## Incident Checklist (First 5 Minutes)

### 1. Identify Scope
- [ ] Is the worker running at all?
- [ ] Is it processing ticks? Crashing/restarting?
- [ ] Is the issue isolated to one environment?
- [ ] When did it last work correctly?

### 2. Collect Basic Info
- [ ] Current commit/version deployed
- [ ] Current config values (`Delta:Enabled`, `Delta:PollIntervalSeconds`)
- [ ] Approximate start time of the incident
- [ ] Any recent changes (deploy, config, SharePoint changes)

### 3. Grab Logs

Look for these key messages:

```bash
# Worker lifecycle
grep -E "(starting|stopping)" logs/worker.log

# Errors
grep -E "(Error|Exception|❌|⚠️)" logs/worker.log

# Recent activity
tail -500 logs/worker.log | grep -E "(Updated|Skipped|Failed|complete)"
```

### 4. Stabilize

If processing is causing harm:

```bash
# Option 1: Disable via config
export Delta__Enabled=false
# Then restart worker

# Option 2: Stop the worker
sudo systemctl stop spx2
# or
docker stop spx2
```

## Common Symptoms

### Worker is running but doing nothing

**Expected if** `Delta:Enabled=false`

**Diagnosis:**
```bash
# Check config
grep -i "enabled" appsettings.json

# Check logs
grep "Delta processing is disabled" logs/worker.log
```

**Resolution:**
1. Set `Delta:Enabled=true`
2. Restart worker
3. Verify logs show "Starting delta processing"

---

### Worker keeps crashing/restarting

**Diagnosis:**
```bash
# Find first exception
grep -A 5 "Unhandled exception" logs/worker.log | head -20

# Check systemd status
sudo systemctl status spx2

# Check restart count
sudo systemctl show spx2 --property=NRestarts
```

**Common causes:**
| Error | Likely Cause | Fix |
|-------|--------------|-----|
| `Invalid tenant id` | Wrong TenantId format | Use GUID without `< >` |
| `Unable to resolve site id` | Wrong SiteUrl | Verify SharePoint URL |
| `Drive not found` | Wrong DriveName | Check library name in SharePoint |
| `401 Unauthorized` | Expired/invalid credentials | Regenerate client secret |
| `Options validation failed` | Invalid config value | Check ranges in [configuration.md](configuration.md) |

**Resolution:**
1. Fix the root cause
2. Set `Delta:Enabled=false` temporarily if needed
3. Restart and verify

---

### High CPU / Tight Loop

**Diagnosis:**
```bash
# Check poll interval
grep -i "pollinterval" appsettings.json

# Count exceptions per minute
grep -c "Exception" logs/worker.log

# Check process CPU
top -p $(pgrep -f Spx.DeltaWorker)
```

**Common causes:**
- `PollIntervalSeconds` too low (< 5)
- Repeated exceptions triggering rapid retries
- Rate limiter at minimum (5/s) with many items

**Resolution:**
1. Increase `Delta:PollIntervalSeconds` to 300+ for production
2. Fix underlying exceptions
3. Reduce `MaxWorkers` if CPU-bound

---

### HTTP 429 Too Many Requests (Throttling)

**Diagnosis:**
```bash
grep "429\|Rate limited\|Reducing rate" logs/worker.log
```

**This is usually self-healing** - the adaptive rate limiter handles it:
1. Reduces rate by 50% (20 → 10 → 5, minimum 5/s)
2. Waits for `Retry-After` duration
3. Gradually increases on success

**If persistent:**
1. Reduce `MaxWorkers` (try 10)
2. Reduce `RateLimitPerSecond` (try 10)
3. Increase `PollIntervalSeconds` (try 7200)

---

### No files being processed

**Diagnosis:**
```bash
# Check if delta returns items
grep "Processing batch" logs/worker.log

# Check state file
cat .state/sharepoint-delta.json | jq .

# Verify SharePoint library has files
# (check manually in SharePoint)
```

**Common causes:**
- Delta link expired (SharePoint keeps ~30 days)
- Wrong `DriveName` configured
- `FolderPath` pointing to empty folder
- All fields already filled (`ForceUpdate=false`)

**Resolution:**
1. Delete state file to force full resync
2. Verify `DriveName` matches SharePoint library
3. Check `FolderPath` if set
4. Set `ForceUpdate=true` temporarily if fields need refresh

---

### Fields not updating in SharePoint

**Diagnosis:**
```bash
# Check for PATCH failures
grep -E "(Failed to update|❌)" logs/worker.log

# Check if fields exist
grep "fields" logs/worker.log | head -5
```

**Common causes:**
| Symptom | Cause | Fix |
|---------|-------|-----|
| All PATCH fail | Missing SharePoint columns | Create columns in library |
| Column name mismatch | Special characters | Use internal names (e.g., `Created_x0020_By`) |
| Permission denied | App lacks Sites.ReadWrite.All | Grant permission in Azure AD |
| Fields skipped | Already filled | Set `ForceUpdate=true` |

---

### State file corrupted

**Symptoms:**
- JSON parse errors in logs
- Worker can't start

**Resolution:**
```bash
# Backup corrupted file
mv .state/sharepoint-delta.json .state/sharepoint-delta.json.corrupted

# Worker will create new file and do full resync
sudo systemctl restart spx2
```

---

## Diagnostic Commands

### From Repo Root

```bash
# Verify environment
dotnet --info

# Build and test
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release -warnaserror
dotnet test SPX2.slnx -c Release

# Format check
dotnet format SPX2.slnx --verify-no-changes
```

### Check Graph API Connectivity

```bash
# Get access token (requires Azure CLI)
az account get-access-token --resource https://graph.microsoft.com

# Test endpoint
curl -H "Authorization: Bearer $TOKEN" \
  "https://graph.microsoft.com/v1.0/sites/root"
```

### Inspect State File

```bash
# View state
cat .state/sharepoint-delta.json | jq .

# Check delta link age
cat .state/sharepoint-delta.json | jq '.savedAtUtc'

# Clear state for full resync
rm .state/sharepoint-delta.json
```

### View Recent Output

```bash
# Last 10 processed items
tail -10 .out/sharepoint-metadata.ndjson | jq .

# Count by status
cat .out/sharepoint-metadata.ndjson | jq -r '.existingFields | keys | length' | sort | uniq -c
```

## Escalation

If you cannot resolve the issue:

1. Collect:
   - Full error logs (last 1000 lines)
   - Configuration (redact secrets)
   - State file contents
   - Recent changes/deploys

2. Create GitHub issue with:
   - Impact description
   - Timeline
   - Error messages
   - Steps to reproduce

## Post-Incident

After resolving:

1. **Document** - Create issue describing:
   - Impact and duration
   - Root cause
   - Mitigation steps
   - Follow-up actions

2. **Prevent** - Consider:
   - Adding monitoring/alerts
   - Improving error messages
   - Adding tests for the failure case

3. **Verify** - Monitor for:
   - Normal processing resuming
   - No recurring errors
   - Expected throughput
