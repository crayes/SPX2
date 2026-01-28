# Configuration

## Delta Settings

The worker binds options from the `Delta` configuration section.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Delta:Enabled` | bool | `false` | Enable/disable delta processing |
| `Delta:PollIntervalSeconds` | int | `5` | Interval between delta checks (1-3600) |
| `Delta:MaxWorkers` | int | `20` | Parallel workers for processing (1-50) |
| `Delta:RateLimitPerSecond` | int | `20` | Max requests per second (1-100) |

### Parallel Processing

The worker uses `Parallel.ForEachAsync` with `MaxWorkers` for concurrent file processing:

- Default: 20 workers (ported from Python `MAX_WORKERS`)
- Recommended: 10-30 for most scenarios
- Higher values may trigger Graph API throttling

### Adaptive Rate Limiting

The `AdaptiveRateLimiter` automatically adjusts request rate:

- Starts at `RateLimitPerSecond` (default: 20/s)
- On HTTP 429: reduces by 50% (20 → 10 → 5, minimum: 5)
- On success: gradually increases (+5, up to 2x initial rate)
- Respects `Retry-After` header from Graph API

Log example:
```
⚠️ Rate limited (429). Reducing rate to 10/s. Waiting 30s...
```

## SharePoint Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `SharePoint:TenantId` | string | — | Azure AD tenant ID |
| `SharePoint:ClientId` | string | — | App registration client ID |
| `SharePoint:ClientSecret` | string | — | App registration secret |
| `SharePoint:SiteUrl` | string | — | SharePoint site URL |
| `SharePoint:DriveName` | string | `Documentos` | Document library name |
| `SharePoint:FolderPath` | string | `""` | Optional subfolder path |
| `SharePoint:MaxItemsPerRun` | int | `500` | Max items per delta tick |
| `SharePoint:ForceUpdate` | bool | `false` | Overwrite existing field values |
| `SharePoint:HttpTimeoutSeconds` | int | `100` | HTTP request timeout |

### ForceUpdate Behavior

- `ForceUpdate: false` (default): Only fills empty fields, preserves existing values
- `ForceUpdate: true`: Overwrites all fields on every run

## Example Configuration

### appsettings.json

```json
{
  "Delta": {
    "Enabled": true,
    "PollIntervalSeconds": 7200,
    "MaxWorkers": 20,
    "RateLimitPerSecond": 20
  },
  "SharePoint": {
    "SiteUrl": "https://rfaasp.sharepoint.com/sites/copilot",
    "DriveName": "Documentos",
    "FolderPath": "",
    "DeltaStateFile": ".state/sharepoint-delta.json",
    "OutputNdjsonPath": ".out/sharepoint-metadata.ndjson",
    "MaxItemsPerRun": 500,
    "ForceUpdate": false,
    "HttpTimeoutSeconds": 100
  }
}
```

### Environment Variables

.NET maps `:` to `__` in environment variables:

```bash
export Delta__Enabled=true
export Delta__PollIntervalSeconds=7200
export Delta__MaxWorkers=20
export Delta__RateLimitPerSecond=20
export SharePoint__TenantId="your-tenant-id"
export SharePoint__ClientId="your-client-id"
export SharePoint__ClientSecret="your-secret"
export SharePoint__SiteUrl="https://contoso.sharepoint.com/sites/docs"
export SharePoint__DriveName="Documentos"
export SharePoint__ForceUpdate=false
```

### User Secrets (Development)

```bash
# Initialize (first time only)
dotnet user-secrets init --project worker/Spx.DeltaWorker

# Set credentials (without < >)
dotnet user-secrets set "SharePoint:TenantId" "your-tenant-guid" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "your-app-id" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "your-secret" --project worker/Spx.DeltaWorker

# Verify
dotnet user-secrets list --project worker/Spx.DeltaWorker
```

## Performance Tuning

### Large Libraries (200,000+ files)

Recommended settings for large document libraries:

```json
{
  "Delta": {
    "Enabled": true,
    "PollIntervalSeconds": 7200,
    "MaxWorkers": 20,
    "RateLimitPerSecond": 20
  },
  "SharePoint": {
    "MaxItemsPerRun": 1000
  }
}
```

| Scenario | MaxWorkers | RateLimitPerSecond | Notes |
|----------|------------|-------------------|-------|
| Conservative | 10 | 10 | Avoid throttling |
| Balanced | 20 | 20 | Default, good for most cases |
| Aggressive | 30 | 30 | May trigger 429s, auto-adjusts |

### First Run vs Incremental

- **First run**: Processes all files (may take hours for large libraries)
- **Subsequent runs**: Delta API returns only changes (minutes)

## Generated Metadata Fields

The worker automatically generates these 15 fields for each file:

| Field | Type | Description |
|-------|------|-------------|
| `TipoDocumento` | string | Document type based on extension |
| `CategoriaInteligente` | string | Smart category |
| `PalavrasChaveIA` | string | Keywords from filename |
| `StatusProcessamento` | string | "Processado" or "Importado EML" |
| `SubpastaOrigem` | string | Parent folder path |
| `CaminhoCompleto` | string | Full path with filename |
| `NomeArquivoLimpo` | string | Filename without extension |
| `ExtensaoArquivo` | string | Extension (uppercase) |
| `TamanhoBytes` | number | File size |
| `DataCriacaoOriginal` | datetime | Creation date |
| `DataModificacaoOriginal` | datetime | Last modified date |
| `DataProcessamentoIA` | datetime | Processing timestamp |
| `CriadoPor` | string | Creator display name |
| `IdadeArquivoDias` | number | Age in days |
| `IdadeArquivoDescricao` | string | Age with unit (dias/meses/anos) |

### Type Classification

| Extension | TipoDocumento | CategoriaInteligente |
|-----------|---------------|----------------------|
| pdf | Contrato | Documento Formal |
| docx, doc | Relatorio | Texto |
| xlsx, xls | Planilha | Financeiro |
| pptx, ppt | Apresentacao | Apresentacao |
| eml, msg | Email | Comunicacao |
| txt, rtf | Documento | Texto |
| mp4, mov | Video | Midia - Video |
| jpg, jpeg, png, gif | Imagem | Midia - Imagem |
| (other) | Outro | Outros |

## SharePoint Column Requirements

Ensure your SharePoint library has these columns created:

1. Go to Document Library Settings → Columns
2. Create each column with matching names and types
3. Columns with spaces use `_x0020_` encoding internally

## Validation

Options use DataAnnotations validation. Invalid values cause startup failure.

## Troubleshooting

### HTTP 429 Too Many Requests

The worker handles this automatically:
1. Reduces rate by 50%
2. Waits for `Retry-After` duration
3. Retries the request

If happening frequently, reduce `MaxWorkers` and `RateLimitPerSecond`.

### HTTP 503 Service Unavailable

Graph API temporary issue. Worker retries with exponential backoff.

### "Invalid tenant id provided"

Check `user-secrets list` - ensure TenantId is a valid GUID without `< >`.
