using Azure;
using Azure.Core;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Storage;
using Azure.ResourceManager.Storage.Models;
using Azure.Storage.Blobs;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace ClaudeMCP.McpTools;

[McpServerToolType]
public sealed class AzureTools
{
    private readonly ILogger<AzureTools> _logger;
    private readonly BlobServiceClient _blobService;
    private readonly LogsQueryClient _logs;
    private readonly ArmClient _arm;

    public AzureTools(
        ILogger<AzureTools> logger,
        BlobServiceClient blobService,
        LogsQueryClient logs,
        ArmClient arm,
        HttpClient http)
    {
        _logger = logger;
        _blobService = blobService;
        _logs = logs;
        _arm = arm;
    }

    /// <summary>
    /// Downloads the content of a text file from Azure Blob Storage and returns it as a string.
    /// </summary>
    /// <remarks>This method retrieves the specified blob from Azure Blob Storage, reads its content into
    /// memory,  and converts it to a string using the specified encoding. The default encoding is UTF-8.</remarks>
    /// <param name="containerName">The name of the Azure Blob Storage container that contains the blob. This value cannot be null or empty.</param>
    /// <param name="blobName">The name of the blob to download. This value cannot be null or empty.</param>
    /// <param name="encoding">The name of the text encoding to use when converting the blob's content to a string.  Supported values are
    /// "utf8" (default), "utf-8", or "ascii". If null or an unsupported value is provided, UTF-8 encoding is used.</param>
    /// <returns>A string containing the content of the downloaded blob.</returns>
    [McpServerTool, Description("Downloads a text file from Azure Blob Storage")]
    public async Task<string> FetchBlobTextAsync(
        string containerName,
        string blobName,
        string? encoding = "utf8"
        )
    {
        var container = _blobService.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        using var stream = new MemoryStream();
        await blob.DownloadToAsync(stream);

        stream.Position = 0;

        return encoding?.ToLowerInvariant() switch
        {
            "utf8" or "utf-8" or null => Encoding.UTF8.GetString(stream.ToArray()),
            "ascii" => Encoding.ASCII.GetString(stream.ToArray()),
            _ => Encoding.UTF8.GetString(stream.ToArray())
        };
    }

    /// <summary>
    /// Executes a KQL (Kusto Query Language) query in Azure Log Analytics and returns the results as a formatted
    /// string.
    /// </summary>
    /// <remarks>This method logs the executed KQL query for informational purposes. The results are formatted
    /// based on the value of the <paramref name="asCsv"/> parameter.</remarks>
    /// <param name="workspaceId">The unique identifier of the Log Analytics workspace where the query will be executed.</param>
    /// <param name="kql">The KQL query to execute.</param>
    /// <param name="timespan">The time range for the query, specified as an ISO 8601 duration (e.g., "P1D" for one day). Defaults to "P1D".</param>
    /// <param name="asCsv">A boolean value indicating the format of the returned results.  <see langword="true"/> to return the results as
    /// a CSV-formatted string; <see langword="false"/> to return the results as a pipe-delimited string. Defaults to
    /// <see langword="true"/>.</param>
    /// <returns>A string containing the query results. If no results are found, the method returns "No results".</returns>
    [McpServerTool, Description("Executes a KQL query in Log Analytics and returns results as text/CSV")]
    public async Task<string> QueryMonitorLogsAsync(
        string workspaceId,
        string kql,
        string timespan = "P1D",
        bool asCsv = true
        )
    {
        var ts = (QueryTimeRange)TimeSpan.Parse(timespan);
        _logger.LogInformation("KQL: {kql}", kql);
        Response<LogsQueryResult> response = await _logs.QueryWorkspaceAsync(workspaceId, kql, ts);

        if (response.Value.AllTables.Count == 0)
        {
            return "No results";
        }

        LogsTable t = response.Value.AllTables[0];

        if (!asCsv)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(" | ", t.Columns.Select(c => c.Name)));

            foreach (LogsTableRow row in t.Rows)
            {
                sb.AppendLine(string.Join(" | ", row.Select(v => v?.ToString() ?? "")));
            }

            return sb.ToString();
        }
        else
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", t.Columns.Select(c => c.Name)));

            foreach (LogsTableRow row in t.Rows)
            {
                sb.AppendLine(string.Join(",", row.Select(v => (v?.ToString() ?? "").Replace(",", ";"))));
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Checks whether encryption is enabled for the Blob service of a specified storage account.
    /// </summary>
    /// <remarks>This method retrieves the encryption settings for the Blob service of the specified storage
    /// account by querying the Azure Resource Manager. Ensure that the caller has appropriate permissions to access the
    /// subscription, resource group, and storage account.</remarks>
    /// <param name="subscriptionId">The subscription ID that contains the storage account.</param>
    /// <param name="resourceGroup">The name of the resource group that contains the storage account.</param>
    /// <param name="storageAccountName">The name of the storage account to check.</param>
    /// <returns>A string indicating the encryption status of the Blob service.  Returns "Encryption BLOB: Turned On" if
    /// encryption is enabled; otherwise, "Encryption BLOB: Turned Off".</returns>
    [McpServerTool, Description("Checks if Storage Account has encryption enabled")]
    public async Task<string> CheckStorageEncryptionAsync(string subscriptionId, string resourceGroup, string storageAccountName)
    {
        SubscriptionResource sub = _arm.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        Response<ResourceGroupResource> rg = sub.GetResourceGroup(resourceGroup);
        Response<StorageAccountResource> storage = await rg.Value.GetStorageAccountAsync(storageAccountName);

        StorageAccountEncryption props = storage.Value.Data.Encryption;
        bool enabled = props.Services?.Blob?.IsEnabled ?? false;

        return enabled
            ? "Encryption BLOB: Turned On"
            : "Encryption BLOB: Turned Off";

    }
}