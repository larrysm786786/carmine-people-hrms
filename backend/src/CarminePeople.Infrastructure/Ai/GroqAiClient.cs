using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CarminePeople.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarminePeople.Infrastructure.Ai;

public class GroqOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "openai/gpt-oss-120b";
    public int MaxTokens { get; set; } = 1024;
}

// Calls Groq's OpenAI-compatible chat completions endpoint. Requires Ai__Groq__ApiKey to be
// configured; until then IsConfigured is false and callers should show a friendly
// "AI assistant not yet configured" message instead of erroring.
public class GroqAiClient : IAiClient
{
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqAiClient> _logger;

    public GroqAiClient(HttpClient httpClient, IOptions<GroqOptions> options, ILogger<GroqAiClient> logger)
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
            throw new InvalidOperationException("Groq API key is not configured (Ai:Groq:ApiKey).");
        }

        var messages = new List<GroqMessage> { new("system", systemPrompt) };
        messages.AddRange(history.Select(m => new GroqMessage(m.Role, m.Content)));

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");
        request.Content = JsonContent.Create(new GroqRequest(_options.Model, messages, _options.MaxTokens));

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Groq API call failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException("The AI assistant is temporarily unavailable. Please try again shortly.");
        }

        var result = await response.Content.ReadFromJsonAsync<GroqResponse>(cancellationToken: ct);
        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    private record GroqRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] List<GroqMessage> Messages,
        [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens);

    private record GroqMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record GroqResponse(
        [property: JsonPropertyName("choices")] List<GroqChoice>? Choices);

    private record GroqChoice(
        [property: JsonPropertyName("message")] GroqMessage? Message);
}
