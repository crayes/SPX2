# Legacy / archived content

The repository historically contained non-.NET components (e.g., a Python consumer and setup tooling). Those are **archived under `tools/`** and are not part of build/test/deploy.

- `tools/legacy-python/` — archived Python consumer snapshot
- `tools/legacy-setup/` — archived setup tooling snapshot
- `tools/corrupted-snapshot/` — snapshots kept for forensics

If you decide to restore or migrate logic from legacy code, treat it as source material only and port the required logic into the .NET worker.
