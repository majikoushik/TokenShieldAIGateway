using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace TokenShield.ProviderAdapters;

public class AzureOpenAiProviderAdapter : IProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AzureOpenAiProviderAdapter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public string ProviderName => "Azure OpenAI";

    public async Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("AzureOpenAiClient");

        var baseUrl = string.IsNullOrEmpty(request.ApiUrl) ? "https://endpoint.openai.azure.com" : request.ApiUrl.TrimEnd('/');
        var deployment = string.IsNullOrEmpty(request.DeploymentName) ? "deployment" : request.DeploymentName;
        var url = $"{baseUrl}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-15-preview";

        var payload = new AzureChatPayload
        {
            Messages = request.Messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var json = JsonSerializer.Serialize(payload);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrEmpty(request.ApiKey))
        {
            httpRequest.Headers.Add("api-key", request.ApiKey);
        }

        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<AzureResponsePayload>(responseJson);

        if (result == null || result.Choices == null || result.Choices.Count == 0)
        {
            throw new HttpRequestException("Azure OpenAI returned an empty choice list.");
        }

        var firstChoice = result.Choices[0];

        return new ModelResponse
        {
            Id = result.Id ?? $"azure-response-{System.Guid.NewGuid():N}",
            Content = firstChoice.Message?.Content ?? "",
            PromptTokens = result.Usage?.PromptTokens ?? 0,
            CompletionTokens = result.Usage?.CompletionTokens ?? 0,
            ModelName = request.ModelName,
            ProviderName = ProviderName
        };
    }

    private class AzureChatPayload
    {
        [JsonPropertyName("messages")]
        public System.Collections.Generic.List<ModelMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }
    }

    private class AzureResponsePayload
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public System.Collections.Generic.List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public AzureUsage? Usage { get; set; }

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

        public class AzureUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }
        }
    }
}
