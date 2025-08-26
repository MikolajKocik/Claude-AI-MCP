using System.Text.Json;

namespace ClaudeMCP.Clients;

public class ClaudeClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.anthropic.com/v1/messages";
    private readonly string _model;

    public ClaudeClient(HttpClient httpClient, string apiKey, string? model = null)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _model = string.IsNullOrWhiteSpace(model) ? "claude-3-haiku-20240307" : model!;
    }

    public async Task<string> AskClaudeAsync(string prompt, CancellationToken ct, double temperature = 0.2, int maxTokens = 2000)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = maxTokens,
            temperature,    
            messages = new[]   
            {
                new { role = "user", content = prompt }
            }
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_baseUrl, requestBody, cancellationToken: ct);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();

        return content ?? "";
    }
}
