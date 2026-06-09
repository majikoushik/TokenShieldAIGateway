# Chat Completions Endpoint

TokenShield exposes a proxy route compatible with the OpenAI Chat Completions API. This document lists the request contract, parameters validation constraints, response payloads, and custom routing metadata parameters.

---

## 1. Request Contract

- **Method**: `POST`
- **Route**: `/v1/chat/completions`
- **Headers**:
  - `x-api-key`: `ts_dev_acmedeveloperkey12345` (Required)
  - `Content-Type`: `application/json`

### Body Schema Parameters
- **model**: `string` (Required). Set to `"auto"` to let TokenShield select the optimal model tier based on rules, or specify a specific deployment model.
- **messages**: `array` (Required). List of chat message objects containing:
  - `role`: `string` (Must be one of: `"system"`, `"user"`, `"assistant"`).
  - `content`: `string` (Required text message).
- **temperature**: `double` (Optional, default 1.0). Must be between `0.0` and `2.0`.
- **max_tokens**: `integer` (Optional). Must be a positive integer.
- **stream**: `boolean` (Required/Optional). Must be `false` or omitted.

---

## 2. Validation Constraints

TokenShield validates incoming requests strictly. Failures return a standard `400 Bad Request` payload:

### 2.1 Message Roles
All objects in the `messages` array must utilize a valid role. If an invalid role name (e.g. `"developer"`) is provided, it is rejected.

### 2.2 Streaming Responses
Streaming (`stream: true`) is currently out-of-scope for the MVP. Requests setting `stream: true` will be rejected:
```json
{
  "error": {
    "message": "Request validation failed.",
    "type": "validation_error",
    "code": "400",
    "details": [
      {
        "field": "Stream",
        "message": "Streaming responses are currently not supported in this version. Set 'stream' to false."
      }
    ]
  }
}
```

---

## 3. Success Response Payload

A successful request returns an OpenAI-compatible completion shape, with an added `routing` block explaining how the gateway processed the call:

```json
{
  "id": "chatcmpl_mock_40915f7690fe4bc18e0dfa2123",
  "object": "chat.completion",
  "created": 1730000000,
  "model": "routed:mock-cheap",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! I am a simulated response from TokenShield AI Gateway..."
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 15,
    "completion_tokens": 20,
    "total_tokens": 35
  },
  "routing": {
    "selectedTier": "cheap",
    "selectedProvider": "Mock Provider",
    "selectedModel": "mock-cheap",
    "matchedRule": "Default Routing",
    "estimatedCost": 0.0000055,
    "fallbackUsed": false,
    "cacheHit": false
  }
}
```

---

## 4. Privacy & Logs Rule
By default, TokenShield **does not save raw prompt text or generated response contents** to standard database tables or log outputs.
To support auditing without violating privacy:
- The prompt is aggregated and mapped to a SHA-256 cryptographic string (`PromptHash`).
- The response text is mapped to a SHA-256 cryptographic string (`ResponseHash`).
- Only hashes, token tallies, and latency parameters are saved in the `AiRequestLogs` database.
