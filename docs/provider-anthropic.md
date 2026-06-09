# Anthropic Provider Adapter

The Anthropic adapter integrates TokenShield with Anthropic's Messages REST API.

---

## Endpoint Configuration

- **Default API Endpoint**: `https://api.anthropic.com/v1/messages` (overridable by the `ApiUrl` field defined on the `ModelProvider` entity).
- **Authentication**: Set using HTTP header `x-api-key: <ApiKey>` where `ApiKey` is resolved securely from configuration secrets.
- **Required Headers**:
  - `anthropic-version: 2023-06-01`
  - `content-type: application/json`

---

## Payload Mappings

Anthropic's Messages API enforces distinct structure rules, which are mapped dynamically:

### System Prompt Translation
Unlike OpenAI, Anthropic's API does not accept `"system"` as a message role inside the `"messages"` array. The adapter scans messages, extracts any message with role `"system"`, and places its text content under a top-level `"system"` parameter in the payload.

### Messages Roles Translation
Non-system messages roles are mapped directly to `"user"` or `"assistant"`.

### Request Example
```json
{
  "model": "claude-3-5-sonnet-20241022",
  "messages": [
    { "role": "user", "content": "Hello!" }
  ],
  "system": "You are a helpful assistant.",
  "max_tokens": 1024,
  "temperature": 0.5
}
```

### Response Example
The adapter parses the text block content structure:
```json
{
  "id": "msg_123",
  "content": [
    {
      "type": "text",
      "text": "Hello, how can I help you?"
    }
  ],
  "usage": {
    "input_tokens": 10,
    "output_tokens": 8
  }
}
```
The adapter extracts prompt/completion token details from `usage.input_tokens` and `usage.output_tokens`.
