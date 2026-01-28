# Configuration

## Delta Settings

The worker binds options from the `Delta` configuration section.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Delta:Enabled` | bool | `false` | Enable/disable delta processing |
| `Delta:PollIntervalSeconds` | int | `5` | Interval between delta checks (1-3600) |

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
    "PollIntervalSeconds": 7200
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
export SharePoint__TenantId="your-tenant-id"
export SharePoint__ClientId="your-client-id"
export SharePoint__ClientSecret="your-secret"
export SharePoint__SiteUrl="https://contoso.sharepoint.com/sites/docs"
export SharePoint__DriveName="Documentos"
export SharePoint__ForceUpdate=false
```

### User Secrets (Development)

```bash
dotnet user-secrets set "SharePoint:TenantId" "<tenant-guid>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientId" "<app-id>" --project worker/Spx.DeltaWorker
dotnet user-secrets set "SharePoint:ClientSecret" "<secret>" --project worker/Spx.DeltaWorker
```

## Generated Metadata Fields

The worker automatically generates these 14 fields for each file:

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
