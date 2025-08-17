namespace Spx.App.Core;

public sealed class AppConfig
{
    public DatabaseConfig Database { get; set; } = new();
    public GraphConfig Graph { get; set; } = new();
    public SharePointConfig SharePoint { get; set; } = new();
    public QueueConfig Queue { get; set; } = new();
}

public sealed class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public string Port { get; set; } = "5432";
    public string Database { get; set; } = "spxdb";
    public string Username { get; set; } = "spx";
    public string Password { get; set; } = "__env__:DB__Password";
    public string SslMode  { get; set; } = "Require";
}

public sealed class GraphConfig
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "__env__:Graph__ClientSecret";
}

public sealed class SharePointConfig
{
    public string SiteDomain { get; set; } = "";
    public string SiteName   { get; set; } = "";
    public string DriveName  { get; set; } = "Documents";
    public string SiteId     { get; set; } = "";
    public string DriveId    { get; set; } = "";
    public string AllowedFolders { get; set; } = "";
}

public sealed class QueueConfig
{
    public string Provider         { get; set; } = "AzureServiceBus";
    public string ConnectionString { get; set; } = "__env__:Queue__ConnectionString";
    public string QueueName        { get; set; } = "spx-driveitem-events";
}
