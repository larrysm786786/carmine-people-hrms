namespace CarminePeople.Application.Common.Interfaces;

public record AiChatMessage(string Role, string Content); // Role: "user" | "assistant"

public interface IAiClient
{
    // Returns the assistant's reply text, or throws if the AI provider is unreachable/unconfigured.
    Task<string> CompleteAsync(string systemPrompt, IReadOnlyList<AiChatMessage> history, CancellationToken ct);
    bool IsConfigured { get; }
}
