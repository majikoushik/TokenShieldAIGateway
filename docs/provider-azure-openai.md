# Azure OpenAI Provider Adapter

The Azure OpenAI adapter integrates TokenShield with deployments of Microsoft Azure OpenAI service.

---

## Endpoint Configuration

- **API Endpoint Format**: `{ApiUrl}/openai/deployments/{DeploymentName}/chat/completions?api-version=2024-02-15-preview`
  - `ApiUrl`: Overridable base endpoint (e.g. `https://acme-openai.openai.azure.com`).
  - `DeploymentName`: The identifier of the deployed model (e.g., `deploy-gpt-4o`).
- **Authentication**: Set using custom HTTP header `api-key: <ApiKey>` where the key is resolved securely from configuration secrets.

---

## Payload Mappings

Request and response JSON formats are structurally identical to the standard OpenAI schema:

### Request Example
```json
{
  "messages": [
    { "role": "user", "content": "What is the capital of France?" }
  ],
  "temperature": 0.5,
  "max_tokens": 800
}
```

### Response Example
```json
{
  "id": "chatcmpl-azure-123",
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "Paris"
      }
    }
  ],
  "usage": {
    "prompt_tokens": 12,
    "completion_tokens": 1
  }
}
```
If Azure OpenAI responds with a non-success status code (e.g. `401 Unauthorized`, `429 Too Many Requests`, `503 Service Unavailable`), the adapter propagates the HTTP failure to trigger retry/fallback policies.
