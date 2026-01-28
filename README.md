# SPX2

SPX2 is a .NET Worker Service for **incremental SharePoint metadata processing** using Microsoft Graph Delta API.

## Features

- **Delta API**: Detects new/modified files incrementally (no full scans)
- **Smart Metadata Generation**: Auto-generates 14 metadata fields for each file
- **PATCH to SharePoint**: Updates SharePoint columns via Graph API
- **Rate Limiting**: Handles HTTP 429/503 with exponential backoff
- **State Persistence**: Saves delta tokens for reliable incremental sync

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

# Configure secrets
dotnet user-secrets set "SharePoint:TenantId" "<tenant-guid>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "<app-id>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "<secret>" --project worker/Spx.DeltaWorker

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
    "PollIntervalSeconds": 7200
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
│  3. MetadataGenerator creates 14 smart fields                  │
│  4. SharePointFieldsUpdater PATCHes SharePoint columns         │
│  5. State saved for next incremental run                       │
└─────────────────────────────────────────────────────────────────┘
```

## Repo Layout

- `worker/Spx.DeltaWorker/` — Production worker service
  - `Application/` — Business logic (DeltaEngine, MetadataGenerator)
  - `Infrastructure/` — Graph API client, state persistence
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

- [Sharepoint_Extrator_14.9](https://github.com/crayes/Sharepoint_Extrator_14.9) — Python version (batch processing, maintenance scripts)
