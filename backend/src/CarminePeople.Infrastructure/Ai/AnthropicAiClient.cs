using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarminePeople.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarminePeople.Infrastructure.Ai;

public class AnthropicOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-sonnet-4-5-20250929";
    public int MaxTokens { get; set; } = 1024;
}

// Calls the Anthropic Messages API directly over HTTP (no first-party .NET SDK exists).
// Requires Ai__Anthropic__ApiKey to be configured; until then IsConfigured is false and
// callers should show a friendly "AI assistant not yet configured" message instead of erroring.
public class AnthropicAiClient : IAiClient
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly AnthropicOptions _options;
    private readonly ILogger<AnthropicAiClient> _logger;

    public AnthropicAiClient(HttpClient httpClient, IOptions<AnthropicOptions> options, ILogger<AnthropicAiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<string> CompleteAsync(string systemPrompt, IReadOnlyList<AiChatMessage> history, CancellationToken ct)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Anthropic API key is not configured (Ai:Anthropic:ApiKey).");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", _options.ApiKey);
        request.Headers.Add("anthropic-version", ApiVersion);
        request.Content = JsonContent.Create(new AnthropicRequest(
            _options.Model,
            _options.MaxTokens,
            systemPrompt,
            history.Select(m => new AnthropicMessage(m.Role, m.Content)).ToList()));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Anthropic API call failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("The AI assistant is temporarily unavailable. Please try again shortly.");
        }

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
        var text = result?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
        return text ?? string.Empty;
    }

    private record AnthropicRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] List<AnthropicMessage> Messages);

    private record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record AnthropicResponse(
        [property: JsonPropertyName("content")] List<AnthropicContentBlock>? Content);

    private record AnthropicContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
