using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TokenShield.ProviderAdapters;

public class OpenAiProviderAdapter : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenAiProviderAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string ProviderName => "OpenAI";

    public async Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("OpenAiClient");

        var baseUrl = string.IsNullOrEmpty(request.ApiUrl) ? "https://api.openai.com/v1" : request.ApiUrl.TrimEnd('/');
        var url = $"{baseUrl}/chat/completions";

        var payload = new OpenAiChatPayload
        {
            Model = request.ModelName,
            Messages = request.Messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(request.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        }

        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<OpenAiResponsePayload>(responseJson);

        if (result == null || result.Choices == null || result.Choices.Count == 0)
        {
            throw new HttpRequestException("OpenAI returned an empty choice list.");
        }

        var firstChoice = result.Choices[0];

        return new ModelResponse
        {
            Id = result.Id ?? $"openai-response-{System.Guid.NewGuid():N}",
            Content = firstChoice.Message?.Content ?? "",
            PromptTokens = result.Usage?.PromptTokens ?? 0,
            CompletionTokens = result.Usage?.CompletionTokens ?? 0,
            ModelName = request.ModelName,
            ProviderName = ProviderName
        };
    }

    private class OpenAiChatPayload
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = null!;

        [JsonPropertyName("messages")]
        public System.Collections.Generic.List<ModelMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }
    }

    private class OpenAiResponsePayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public System.Collections.Generic.List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAiUsage? Usage { get; set; }

        public class Choice
        {
            [JsonPropertyName("message")]
            public Message? Message { get; set; }
        }

        public class Message
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        public class OpenAiUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }
        }
    }
}
