namespace Spx.DeltaWorker.Application;

/// <summary>
/// Gera metadados inteligentes para arquivos do SharePoint.
/// Portado do Python (Sharepoint_Extrator_14.9/sharepoint_ultra/utils.py)
/// </summary>
public static class MetadataGenerator
{
    private static readonly Dictionary<string, string> TipoDocumentoMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = "Contrato",
        ["docx"] = "Relatorio",
        ["doc"] = "Relatorio",
        ["xlsx"] = "Planilha",
        ["xls"] = "Planilha",
        ["pptx"] = "Apresentacao",
        ["ppt"] = "Apresentacao",
        ["eml"] = "Email",
        ["msg"] = "Email",
        ["txt"] = "Documento",
        ["rtf"] = "Documento",
        ["mp4"] = "Video",
        ["mov"] = "Video",
        ["jpg"] = "Imagem",
        ["jpeg"] = "Imagem",
        ["png"] = "Imagem",
        ["gif"] = "Imagem"
    };

    private static readonly Dictionary<string, string> CategoriaMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["eml"] = "Comunicacao",
        ["msg"] = "Comunicacao",
        ["pdf"] = "Documento Formal",
        ["docx"] = "Texto",
        ["doc"] = "Texto",
        ["xlsx"] = "Financeiro",
        ["xls"] = "Financeiro",
        ["pptx"] = "Apresentacao",
        ["ppt"] = "Apresentacao",
        ["txt"] = "Texto",
        ["rtf"] = "Texto",
        ["mp4"] = "Midia - Video",
        ["mov"] = "Midia - Video",
        ["jpg"] = "Midia - Imagem",
        ["jpeg"] = "Midia - Imagem",
        ["png"] = "Midia - Imagem",
        ["gif"] = "Midia - Imagem"
    };

    /// <summary>
    /// Gera os 14 campos de metadados para um arquivo.
    /// </summary>
    public static Dictionary<string, object> GerarMetadados(
        string fileName,
        string parentPath,
        long sizeBytes,
        DateTimeOffset? createdDateTime,
        DateTimeOffset? lastModifiedDateTime,
        string? createdByDisplayName)
    {
        var extensao = GetExtension(fileName);
        var agora = DateTimeOffset.UtcNow;
        var idadeDias = createdDateTime.HasValue
            ? Math.Max(0, (int)Math.Floor((agora - createdDateTime.Value).TotalDays))
            : 0;
        var idadeDescricao = FormatAgeDescription(idadeDias);

        var isEmail = extensao.Equals("eml", StringComparison.OrdinalIgnoreCase) ||
                      extensao.Equals("msg", StringComparison.OrdinalIgnoreCase);

        return new Dictionary<string, object>
        {
            ["TipoDocumento"] = ClassificaTipoDocumento(extensao),
            ["StatusProcessamento"] = isEmail ? "Importado EML" : "Processado",
            ["PalavrasChaveIA"] = GerarPalavrasChave(fileName, isEmail),
            ["SubpastaOrigem"] = string.IsNullOrWhiteSpace(parentPath) ? "Raiz" : parentPath,
            ["DataProcessamentoIA"] = agora.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["CategoriaInteligente"] = ClassificaCategoria(extensao),
            ["CriadoPor"] = createdByDisplayName ?? "Sistema",
            ["DataModificacaoOriginal"] = (lastModifiedDateTime ?? agora).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["CaminhoCompleto"] = string.IsNullOrWhiteSpace(parentPath)
                ? fileName
                : $"{parentPath}/{fileName}",
            ["NomeArquivoLimpo"] = GetFileNameWithoutExtension(fileName),
            ["TamanhoBytes"] = sizeBytes,
            ["ExtensaoArquivo"] = extensao.ToUpperInvariant(),
            ["DataCriacaoOriginal"] = (createdDateTime ?? agora).ToString("yyyy-MM-ddTHH:mm:ss"),
            ["IdadeArquivoDias"] = idadeDias,
            ["IdadeArquivoDescricao"] = idadeDescricao
        };
    }

    /// <summary>
    /// Classifica o tipo de documento baseado na extensão.
    /// </summary>
    public static string ClassificaTipoDocumento(string extensao)
    {
        return TipoDocumentoMap.TryGetValue(extensao, out var tipo) ? tipo : "Outro";
    }

    /// <summary>
    /// Determina categoria inteligente do arquivo.
    /// </summary>
    public static string ClassificaCategoria(string extensao)
    {
        return CategoriaMap.TryGetValue(extensao, out var cat) ? cat : "Outros";
    }

    /// <summary>
    /// Gera palavras-chave inteligentes baseadas no nome do arquivo.
    /// </summary>
    public static string GerarPalavrasChave(string fileName, bool isEmail = false)
    {
        try
        {
            var nomeSemExt = GetFileNameWithoutExtension(fileName);

            if (isEmail)
            {
                var partes = nomeSemExt
                    .Replace('-', ' ')
                    .Replace('_', ' ')
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => p.Length > 2)
                    .Take(5);

                return partes.Any() ? string.Join(", ", partes) : "email";
            }

            var nomeLimpo = nomeSemExt
                .Replace('.', ' ')
                .Replace('_', ' ')
                .Replace('-', ' ');

            var palavras = nomeLimpo
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length > 2);

            // Remove duplicatas (case-insensitive)
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var palavrasUnicas = new List<string>();

            foreach (var palavra in palavras)
            {
                if (seen.Add(palavra))
                {
                    palavrasUnicas.Add(palavra);
                }
            }

            return string.Join(", ", palavrasUnicas.Take(8));
        }
        catch
        {
            return "documento";
        }
    }

    private static string GetExtension(string fileName)
    {
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 && lastDot < fileName.Length - 1
            ? fileName[(lastDot + 1)..]
            : "SEM_EXT";
    }

    private static string GetFileNameWithoutExtension(string fileName)
    {
        var lastDot = fileName.LastIndexOf('.');
        return lastDot > 0 ? fileName[..lastDot] : fileName;
    }

    private static string FormatAgeDescription(int idadeDias)
    {
        if (idadeDias < 30)
        {
            return idadeDias == 1 ? "1 dia" : $"{idadeDias} dias";
        }

        if (idadeDias < 365)
        {
            var meses = Math.Max(1, idadeDias / 30);
            return meses == 1 ? "1 mes" : $"{meses} meses";
        }

        var anos = Math.Max(1, idadeDias / 365);
        return anos == 1 ? "1 ano" : $"{anos} anos";
    }
}
