using System.ComponentModel.DataAnnotations;

namespace Spx.DeltaWorker.Configuration;

public sealed class SharePointOptions
{
    public const string SectionName = "SharePoint";

    [Required]
    public string TenantId { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Base site URL, e.g. https://rfaasp.sharepoint.com/sites/copilot
    /// </summary>
    [Required]
    [Url]
    public string SiteUrl { get; init; } = string.Empty;

    /// <summary>
    /// Document library / drive name, e.g. "Documentos".
    /// </summary>
    [Required]
    public string DriveName { get; init; } = "Documentos";

    /// <summary>
    /// Optional folder path within the drive (relative). Examples: "" (root), "/Shared", "Invoices/2026".
    /// </summary>
    public string FolderPath { get; init; } = string.Empty;

    /// <summary>
    /// Relative path (from content root) to persist delta cursors.
    /// </summary>
    [Required]
    public string DeltaStateFile { get; init; } = ".state/sharepoint-delta.json";

    /// <summary>
    /// Relative path (from content root) to write extracted metadata as newline-delimited JSON.
    /// </summary>
    [Required]
    public string OutputNdjsonPath { get; init; } = ".out/sharepoint-metadata.ndjson";

    [Range(1, 100_000)]
    public int MaxItemsPerRun { get; init; } = 500;

    public string[] IncludeFields { get; init; } = [];

    [Range(1, 300)]
    public int HttpTimeoutSeconds { get; init; } = 100;
}