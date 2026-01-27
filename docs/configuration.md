# Configuration

## Delta settings

The worker binds options from the `Delta` configuration section.

Current settings (see `DeltaOptions`):

- `Delta:Enabled` (bool, default `false`)
- `Delta:PollIntervalSeconds` (int, default `5`, range `1..3600`)

### appsettings example

```json
{
  "Delta": {
    "Enabled": true,
    "PollIntervalSeconds": 5
  },
  "SharePoint": {
    "SiteUrl": "https://rfaasp.sharepoint.com/sites/copilot",
    "DriveName": "Documentos",
    "FolderPath": "",
    "DeltaStateFile": ".state/sharepoint-delta.json",
    "OutputNdjsonPath": ".out/sharepoint-metadata.ndjson",
    "MaxItemsPerRun": 500,
    "IncludeFields": ["MetaTags"],
    "HttpTimeoutSeconds": 100
  }
}
```

### Environment variables

.NET maps `:` to `__` in environment variables:

- `Delta__Enabled=true`
- `Delta__PollIntervalSeconds=15`

For SharePoint (client credentials / app registration):

- `SharePoint__TenantId=...`
- `SharePoint__ClientId=...`
- `SharePoint__ClientSecret=...`
- `SharePoint__SiteUrl=https://rfaasp.sharepoint.com/sites/copilot`
- `SharePoint__DriveName=Documentos`
- `SharePoint__MaxItemsPerRun=500`
- `SharePoint__IncludeFields__0=MetaTags`

### Validation behavior

Options use DataAnnotations validation. Invalid values cause the host to fail startup.

## Secrets (recommended)

For local dev, prefer `dotnet user-secrets` instead of committing secrets:

```bash
dotnet user-secrets set "SharePoint:TenantId" "<tenant-guid>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "<app-id-guid>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "<secret>" --project worker/Spx.DeltaWorker
```
