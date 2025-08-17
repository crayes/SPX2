# SPX Admin (WinForms, modular)

## Como usar
1. Extraia o zip para `C:\spx\admin` (ou outra pasta).
2. Abra um **PowerShell**.
3. Rode:
   ```powershell
   dotnet build -c Release .\Spx.App.WinForms\Spx.App.WinForms.csproj
   dotnet run   -c Release --project .\Spx.App.WinForms\Spx.App.WinForms.csproj
   ```
4. Na tela, use os botões **Testar DB / Testar ASB / Testar Graph / Reset & Smoke**.

## Pré-requisitos
- .NET 8 Desktop Runtime/SDK
- Segredos em variáveis de ambiente (ou Credential Manager) conforme seu setup:
  - `DB__Password`, `Queue__ConnectionString`, `Graph__ClientSecret`
- Config canônica em `C:\spx\appsettings.json` (sem segredos).

## Estrutura
- `Spx.App.Core`       → contratos e modelos (AppConfig, interfaces)
- `Spx.App.Infrastructure` → implementações (Npgsql, Service Bus, Graph)
- `Spx.App.WinForms`   → UI leve com DI/Host
