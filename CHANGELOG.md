# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive documentation with Mermaid diagrams
- Operations guide with monitoring and deployment instructions
- This changelog file

## [1.0.0] - 2026-01-28

### Added
- **SharePoint Delta Engine**: Full implementation of delta-based metadata synchronization
- **Parallel Processing**: `Parallel.ForEachAsync` with configurable `MaxWorkers` (default: 20)
- **Adaptive Rate Limiting**: Auto-adjusts request rate on HTTP 429 responses
- **15 Metadata Fields**: Automatic generation based on filename and path
  - TipoDocumento, CategoriaInteligente, PalavrasChaveIA
  - StatusProcessamento, SubpastaOrigem, CaminhoCompleto
  - NomeArquivoLimpo, ExtensaoArquivo, TamanhoBytes
  - DataCriacaoOriginal, DataModificacaoOriginal, DataProcessamentoIA
  - CriadoPor, IdadeArquivoDias, IdadeArquivoDescricao
- **Selective Updates**: Only patches empty fields by default (`ForceUpdate=false`)
- **State Persistence**: Delta cursor saved to JSON file for incremental sync
- **NDJSON Output**: Metadata history appended for audit/debug
- **VS Code Tasks**: Build, run, and test tasks in `.vscode/tasks.json`

### Changed
- Updated .NET SDK to 10.0 (via `global.json`)
- Improved logging with emojis for quick visual scanning (✅ ⏭️ ❌ ⚠️)
- Enhanced error handling with retry logic and exponential backoff

### Documentation
- README with Quick Start guide
- Configuration reference with all options
- Architecture diagrams (Mermaid)
- Operations guide with monitoring recommendations
- Runbook for incident response
- Release and deployment guide

## [0.1.0] - 2026-01-26

### Added
- Initial project structure
- .NET Worker Service template
- Basic Delta API integration (placeholder)
- CI/CD with GitHub Actions
- Project documentation framework

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0 | 2026-01-28 | Full delta sync, parallel processing, 15 metadata fields |
| 0.1.0 | 2026-01-26 | Initial project setup |

## Upgrade Notes

### Upgrading to 1.0.0

1. **New Configuration Options**: Add these to your `appsettings.json`:
   ```json
   {
     "Delta": {
       "MaxWorkers": 20,
       "RateLimitPerSecond": 20
     }
   }
   ```

2. **SharePoint Columns**: Ensure your document library has all 15 columns created. See [configuration.md](configuration.md#sharepoint-column-requirements).

3. **State File Location**: Default changed to `.state/sharepoint-delta.json`. Update if using custom path.

4. **First Run**: After upgrade, the delta link format may have changed. Consider deleting the state file to force a full resync.

## Related

- [Configuration Guide](configuration.md)
- [Architecture Overview](architecture.md)
- [Operations Guide](operations.md)
