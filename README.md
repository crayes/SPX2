# SPX2

SPX2 is a .NET Worker Service for **incremental SharePoint metadata processing** using Microsoft Graph Delta API.

## Features

- **Delta API**: Detects new/modified files incrementally (no full scans)
- **Smart Metadata Generation**: Auto-generates 14 metadata fields for each file
- **PATCH to SharePoint**: Updates SharePoint columns via Graph API
- **Parallel Processing**: Configurable workers (default: 20) for high throughput
- **Adaptive Rate Limiting**: Auto-adjusts request rate on HTTP 429 (ported from Python)
- **Retry with Backoff**: Handles HTTP 429/503 with exponential backoff
- **State Persistence**: Saves delta tokens for reliable incremental sync

## Performance

Optimized for large document libraries (200,000+ files):

| Mode | Throughput | Notes |
|------|------------|-------|
| Sequential | ~2 files/sec | Single thread |
| Parallel (20 workers) | ~40 files/sec | Default configuration |
| With Delta | Only changes | After initial sync, processes only new/modified files |

## Generated Metadata Fields

| Field | Description |
|-------|-------------|
| TipoDocumento | Document type (Contrato, Relatorio, Planilha, Email, etc.) |
| CategoriaInteligente | Smart category (Documento Formal, Financeiro, Comunicacao, etc.) |
| PalavrasChaveIA | AI-generated keywords from filename |
| StatusProcessamento | Processing status (Processado, Importado EML) |
| SubpastaOrigem | Source subfolder path |
| CaminhoCompleto | Full path including filename |
| NomeArquivoLimpo | Filename without extension |
| ExtensaoArquivo | File extension (uppercase) |
| TamanhoBytes | File size in bytes |
| DataCriacaoOriginal | Original creation date |
| DataModificacaoOriginal | Original modification date |
| DataProcessamentoIA | Processing timestamp |
| CriadoPor | Created by (user display name) |
| IdadeArquivoDias | File age in days |

## Requirements

- .NET SDK pinned by `global.json`
- Azure AD App Registration with Graph API permissions:
  - `Sites.ReadWrite.All`
  - `Files.ReadWrite.All`

## Quick Start

```bash
# Restore and build
dotnet restore SPX2.slnx
dotnet build SPX2.slnx -c Release

# Configure secrets (without < >)
dotnet user-secrets init --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:TenantId" "your-tenant-guid" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "your-app-id" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "your-secret" --project worker/Spx.DeltaWorker

# Run
dotnet run --project worker/Spx.DeltaWorker
```

## Configuration

See [docs/configuration.md](docs/configuration.md) for full options.

Minimal `appsettings.json`:

```json
{
  "Delta": {
    "Enabled": true,
    "PollIntervalSeconds": 7200,
    "MaxWorkers": 20,
    "RateLimitPerSecond": 20
  },
  "SharePoint": {
    "SiteUrl": "https://contoso.sharepoint.com/sites/documents",
    "DriveName": "Documentos",
    "ForceUpdate": false
  }
}
```

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     SPX2 Worker Service                         │
├─────────────────────────────────────────────────────────────────┤
│  1. Timer triggers every N seconds (PollIntervalSeconds)       │
│  2. Delta API detects new/modified files                       │
│  3. Parallel.ForEachAsync processes batch with N workers       │
│  4. AdaptiveRateLimiter controls request rate (auto-adjusts)   │
│  5. MetadataGenerator creates 14 smart fields                  │
│  6. SharePointFieldsUpdater PATCHes SharePoint columns         │
│  7. State saved for next incremental run                       │
└─────────────────────────────────────────────────────────────────┘
```

## Adaptive Rate Limiting

Ported from Python (`sharepoint_ultra/rate_limiter.py`):

- Starts at configured rate (default: 20 req/s)
- On HTTP 429: reduces rate by 50% (20 → 10 → 5)
- On success: gradually increases rate
- Respects `Retry-After` header from Graph API

```
⚠️ Rate limited (429). Reducing rate to 10/s. Waiting 30s...
```

## Repo Layout

- `worker/Spx.DeltaWorker/` — Production worker service
  - `Application/` — Business logic (DeltaEngine, MetadataGenerator)
  - `Infrastructure/` — Graph API client, RateLimiter, state persistence
  - `Configuration/` — Options classes
  - `Hosting/` — DI setup
- `tests/` — xUnit tests
- `docs/` — Documentation

## Documentation

- [Configuration](docs/configuration.md)
- [Runbook](docs/runbook.md)
- [Release/Deploy](docs/release-deploy.md)
- [Development](docs/development.md)

## Related Projects

- [Sharepoint_Extrator_14.9](https://github.com/crayes/Sharepoint_Extrator_14.9) — Python version (maintenance: empty folders, old files cleanup)
