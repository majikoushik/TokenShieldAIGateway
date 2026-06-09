# OpenAI Provider Adapter

The OpenAI adapter integrates TokenShield with OpenAI's official chat completions endpoints.

---

## Endpoint Configuration

- **Default API Endpoint**: `https://api.openai.com/v1/chat/completions` (overridable by the `ApiUrl` field defined on the `ModelProvider` entity).
- **Authentication**: Set using HTTP header `Authorization: Bearer <ApiKey>` where `ApiKey` is resolved via the configured secret reference.

---

## JSON Payloads Mapping

### Request Mapping
Generates OpenAI-compatible completions payloads:
```json
{
  "model": "gpt-4o-mini",
  "messages": [
    { "role": "system", "content": "You are a helpful assistant." },
    { "role": "user", "content": "What is 2+2?" }
  ],
  "temperature": 0.7,
  "max_tokens": 1000
}
```

### Response Mapping
Extracts returned text content and token usage from choices:
```json
{
  "id": "chatcmpl-923",
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "4"
      }
    }
  ],
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 1
  }
}
```
If choice content is empty or model parameters fail to parse, the adapter throws a standard `HttpRequestException` for retry/fallback handling.
