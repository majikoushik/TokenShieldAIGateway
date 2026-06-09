# Provider Adapters

TokenShield AI Gateway utilizes a provider adapter abstraction to translate generic completion requests into provider-specific payloads and call external LLM endpoints.

---

## The Provider Adapter Contract

All concrete provider integrations implement the `IProviderAdapter` interface defined in the `TokenShield.ProviderAdapters` package:

```csharp
public interface IProviderAdapter
{
    string ProviderName { get; }
    Task<ModelResponse> CompleteChatAsync(ModelRequest request, CancellationToken cancellationToken);
}
```

- **`ModelRequest`**: Encapsulates common parameters including target model name, deployment name, message list (role/content), temperature, max tokens, credentials, and endpoints URL.
- **`ModelResponse`**: Standardizes returned payloads containing prompt/completion tokens, returned text content, and model metadata.

---

## Configuration Controls

By default, real provider calls are disabled to keep local development and testing safe. This is managed via `appsettings.json`:

```json
{
  "ProviderSettings": {
    "EnableRealCalls": false
  }
}
```

- **`EnableRealCalls = false`**: The `ProviderAdapterFactory` intercepts all adapter queries and redirects them to the `MockProviderAdapter`.
- **`EnableRealCalls = true`**: The factory resolves actual provider adapters (OpenAI, Azure OpenAI, Anthropic, or Mock) based on the resolved model's provider.

---

## Secure Credentials Resolution

API keys and passwords are not stored in raw format within the database. The `ModelProvider` entity holds an `ApiKeySecretRef` string (e.g. `kv-secret-provider-openai`). When a real provider call is made, the credential is load dynamically from configuration secrets:

```json
{
  "ProviderSettings": {
    "Secrets": {
      "kv-secret-provider-openai": "sk-proj-...",
      "kv-secret-provider-anthropic": "sk-ant-...",
      "kv-secret-provider-azure-openai": "azure-key-..."
    }
  }
}
```

If the reference is missing or holds an empty value when real calls are enabled, the request fails with a controlled `InvalidOperationException` resulting in a safe `502 Bad Gateway` API response.
