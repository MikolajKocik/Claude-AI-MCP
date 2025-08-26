using Azure.Core;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.ResourceManager;
using Azure.Storage.Blobs;
using ClaudeMCP.Clients;
using ClaudeMCP.McpTools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
    ?? throw new InvalidOperationException("ANTHROPIC_API_KEY not found");

var claudeModel = Environment.GetEnvironmentVariable("CLAUDE_MODEL")
    ?? "claude-3-haiku-20240307";

// add mcp server
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.AddHttpClient();
builder.Services.AddSingleton(provider =>
{
    var http = provider.GetRequiredService<HttpClient>();
    return new ClaudeClient(http, anthropicKey, claudeModel);
});

builder.Services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
builder.Services.AddSingleton(provider =>
{
    var endpoint = Environment.GetEnvironmentVariable("AZURE_BLOB_ENDPOINT")
        ?? throw new InvalidOperationException("Brak AZURE_BLOB_ENDPOINT");
    
    return new BlobServiceClient(new Uri(endpoint), provider.GetRequiredService<TokenCredential>());
});

builder.Services.AddSingleton(provider =>
    new LogsQueryClient(provider.GetRequiredService<TokenCredential>()));

builder.Services.AddSingleton(provider =>
    new ArmClient(provider.GetRequiredService<TokenCredential>()));

builder.Services.AddSingleton<ComplianceTools>();
builder.Services.AddSingleton<AzureTools>();

await builder.Build().RunAsync();