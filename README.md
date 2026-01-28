# SPX2

SPX2 is a .NET Worker Service that synchronizes SharePoint document metadata using Microsoft Graph Delta API. It automatically detects new/modified files and generates intelligent metadata for legal documents.

## Features

- **Delta API Sync** - Efficient incremental sync using Microsoft Graph delta tokens
- **Smart Metadata Generation** - Automatically generates 14 metadata fields based on filename and path
- **PATCH to SharePoint** - Updates SharePoint list item fields directly
- **Adaptive Rate Limiting** - Avoids 429 errors with intelligent throttling
- **Retry Logic** - Handles transient errors with exponential backoff

## Requirements

- .NET SDK 10.0+ (pinned by `global.json`)
- Azure App Registration with Microsoft Graph permissions:
  - `Sites.ReadWrite.All` (Application)

Check your SDK:

```bash
dotnet --info
```

## Quick Start

### 1. Clone and Restore

```bash
git clone https://github.com/crayes/SPX2.git
cd SPX2
dotnet restore SPX2.slnx
dotnet build SPX2.slnx
```

> **Note:** The repository includes `nuget.config` which automatically configures NuGet.org as package source.

### 2. Configure Secrets

Use `dotnet user-secrets` for local development (never commit secrets!):

```bash
# Initialize user-secrets (only needed once)
dotnet user-secrets init --project worker/Spx.DeltaWorker

# Configure Azure credentials (replace with your actual values)
dotnet user-secrets set "SharePoint:TenantId" "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "your-client-secret" --project worker/Spx.DeltaWorker

# Enable Delta processing
dotnet user-secrets set "Delta:Enabled" "true" --project worker/Spx.DeltaWorker
dotnet user-secrets set "Delta:PollIntervalSeconds" "30" --project worker/Spx.DeltaWorker
```

**Important:** Do NOT include `< >` in your values - they are just placeholders in documentation.

### 3. Run

```bash
# Normal run
dotnet run --project worker/Spx.DeltaWorker

# With debug logging
dotnet run --project worker/Spx.DeltaWorker -- --Logging:LogLevel:Default=Debug
```

## Generated Metadata Fields

The worker generates 14 metadata fields for each document:

| Field | Description | Example |
|-------|-------------|--------|
| `TipoDocumento` | Document type by extension | `Contrato`, `Relatorio`, `Email` |
| `CategoriaInteligente` | Intelligent category | `Documento Formal`, `Comunicacao` |
| `PalavrasChaveIA` | Keywords from filename | `contrato, locacao, imovel` |
| `StatusProcessamento` | Processing status | `Processado` |
| `SubpastaOrigem` | Parent folder name | `Contratos 2024` |
| `CaminhoCompleto` | Full path in library | `/sites/copilot/Docs/Contratos` |
| `NomeArquivoLimpo` | Filename without extension | `Contrato de Locação` |
| `ExtensaoArquivo` | File extension | `.pdf` |
| `TamanhoBytes` | File size | `1048576` |
| `DataCriacaoOriginal` | Creation date | `2024-01-15T10:30:00Z` |
| `DataModificacaoOriginal` | Modified date | `2024-06-20T14:45:00Z` |
| `DataProcessamentoIA` | Processing timestamp | `2024-06-21T08:00:00Z` |
| `CriadoPor` | Created by | `João Silva` |
| `IdadeArquivoDias` | Age in days | `157` |

## Configuration

See [docs/configuration.md](docs/configuration.md) for detailed configuration options.

### Key Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Delta:Enabled` | `false` | Enable/disable delta processing |
| `Delta:PollIntervalSeconds` | `5` | Interval between delta checks |
| `SharePoint:ForceUpdate` | `false` | Overwrite existing field values |
| `SharePoint:SiteUrl` | - | SharePoint site URL |
| `SharePoint:DriveName` | `Documentos` | Document library name |

### Environment Variables

For production, use environment variables:

```bash
export Delta__Enabled=true
export Delta__PollIntervalSeconds=7200
export SharePoint__TenantId=xxx
export SharePoint__ClientId=xxx
export SharePoint__ClientSecret=xxx
```

## Troubleshooting

### NuGet Restore Fails

If `dotnet restore` fails with "Unable to resolve" errors, verify NuGet source:

```bash
dotnet nuget list source
```

Should show `nuget.org`. If empty:

```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Invalid Tenant ID

Error: `Invalid tenant id provided`

- Verify you're using the actual GUID, not placeholder text
- Find your Tenant ID: Azure Portal → Microsoft Entra ID → Overview → Tenant ID

### Rate Limiting (429)

The worker includes adaptive rate limiting. If you still see 429 errors:

1. Increase `Delta:PollIntervalSeconds`
2. Reduce concurrent requests in code

## Repo Layout

```
SPX2/
├── worker/Spx.DeltaWorker/     # Production worker service
│   ├── Application/            # Business logic (DeltaEngine, MetadataGenerator)
│   ├── Infrastructure/         # Graph API client, state stores
│   ├── Configuration/          # Options classes
│   └── Hosting/                # DI extensions
├── tests/                      # xUnit tests
├── docs/                       # Documentation
├── tools/                      # Legacy/archived (not production)
├── nuget.config                # NuGet package source config
└── SPX2.slnx                   # Solution file
```

## Related Projects

- [Sharepoint_Extrator_14.9](https://github.com/crayes/Sharepoint_Extrator_14.9) - Python version (maintenance scripts)

## CI

GitHub Actions runs on pushes/PRs to `main`:

- `dotnet restore/build/test`
- `dotnet format --verify-no-changes`

See [docs/development.md](docs/development.md) for development guidelines.
