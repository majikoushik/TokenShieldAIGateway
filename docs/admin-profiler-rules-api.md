# Profiler Rules Admin API

This document describes the administrative API endpoints for managing Profiler Rules. Profiler Rules configure the production-grade `ConfigurableRuleBasedTaskClassifier` to match user prompts based on phrases or regex patterns.

## Base Path
`GET /admin/profilerrules`

All endpoints require authentication via the `x-api-key` header with an admin-level key or valid JWT token (depending on configured auth).

---

## 1. List Profiler Rules
Retrieves all profiler rules for the active tenant.

`GET /admin/profilerrules`

### Response
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Translation Detection",
    "targetTaskType": "translation",
    "phrases": ["translate", "in spanish"],
    "regexPatterns": ["\\b(translate|translation)\\b"],
    "confidence": 0.8,
    "priority": 10,
    "isActive": true,
    "createdAtUtc": "2023-11-01T12:00:00Z",
    "updatedAtUtc": "2023-11-01T12:00:00Z"
  }
]
```

---

## 2. Get Profiler Rule
Retrieves a specific rule.

`GET /admin/profilerrules/{id}`

### Response
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Translation Detection",
  ...
}
```

---

## 3. Create Profiler Rule
Creates a new profiler rule.

`POST /admin/profilerrules`

### Request Body
```json
{
  "name": "Summarization Rule",
  "targetTaskType": "summarization",
  "phrases": ["summarize", "tldr"],
  "regexPatterns": ["(?i)summary"],
  "confidence": 0.7,
  "priority": 5,
  "isActive": true
}
```

---

## 4. Update Profiler Rule
Updates an existing profiler rule.

`PUT /admin/profilerrules/{id}`

### Request Body
```json
{
  "name": "Summarization Rule Updated",
  "targetTaskType": "summarization",
  "phrases": ["summarize", "tldr", "shorten"],
  "regexPatterns": ["(?i)summary"],
  "confidence": 0.9,
  "priority": 6,
  "isActive": true
}
```

---

## 5. Delete Profiler Rule
Deletes (soft-delete) a profiler rule.

`DELETE /admin/profilerrules/{id}`

Returns `204 No Content` on success.

---

## 6. Test Profiler Rule
Tests a prompt against the provided phrases and regex patterns to evaluate if it matches. This is used by the admin console to simulate rules before saving them.

`POST /admin/profilerrules/test`

### Request Body
```json
{
  "prompt": "Can you summarize this text for me?",
  "targetTaskType": "summarization",
  "phrases": ["summarize", "tldr"],
  "regexPatterns": [],
  "confidence": 0.8
}
```

### Response
```json
{
  "isMatch": true,
  "matchReason": "Matched phrase: 'summarize'",
  "targetTaskType": "summarization",
  "confidence": 0.8
}
```
