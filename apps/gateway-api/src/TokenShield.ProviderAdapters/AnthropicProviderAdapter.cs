using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TokenShield.ProviderAdapters;

public class AnthropicProviderAdapter : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AnthropicProviderAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string ProviderName => "Anthropic";

    public async Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("AnthropicClient");

        var baseUrl = string.IsNullOrEmpty(request.ApiUrl) ? "https://api.anthropic.com/v1" : request.ApiUrl.TrimEnd('/');
        var url = $"{baseUrl}/messages";

        // Extract system prompt if any, and filter user/assistant messages
        var systemPrompt = request.Messages
            .FirstOrDefault(m => m.Role.ToLowerInvariant() == "system")?.Content;

        var messages = request.Messages
            .Where(m => m.Role.ToLowerInvariant() != "system")
            .Select(m => new ModelMessage { Role = m.Role.ToLowerInvariant(), Content = m.Content })
            .ToList();

        var payload = new AnthropicChatPayload
        {
            Model = request.ModelName,
            Messages = messages,
            System = systemPrompt,
            MaxTokens = request.MaxTokens ?? 1024, // Anthropic requires max_tokens
            Temperature = request.Temperature
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(request.ApiKey))
        {
            httpRequest.Headers.Add("x-api-key", request.ApiKey);
        }
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<AnthropicResponsePayload>(responseJson);

        if (result == null || result.Content == null || result.Content.Count == 0)
        {
            throw new HttpRequestException("Anthropic returned an empty content list.");
        }

        var firstContent = result.Content[0];

        return new ModelResponse
        {
            Id = result.Id ?? $"anthropic-response-{System.Guid.NewGuid():N}",
            Content = firstContent.Text ?? "",
            PromptTokens = result.Usage?.InputTokens ?? 0,
            CompletionTokens = result.Usage?.OutputTokens ?? 0,
            ModelName = request.ModelName,
            ProviderName = ProviderName
        };
    }

    private class AnthropicChatPayload
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        [JsonPropertyName("messages")]
        public System.Collections.Generic.List<ModelMessage> Messages { get; set; } = new();

        [JsonPropertyName("system")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? System { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }
    }

    private class AnthropicResponsePayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("content")]
        public System.Collections.Generic.List<ContentItem>? Content { get; set; }

        [JsonPropertyName("usage")]
        public AnthropicUsage? Usage { get; set; }

        public class ContentItem
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        public class AnthropicUsage
        {
            [JsonPropertyName("input_tokens")]
            public int InputTokens { get; set; }

            [JsonPropertyName("output_tokens")]
            public int OutputTokens { get; set; }
        }
    }
}
