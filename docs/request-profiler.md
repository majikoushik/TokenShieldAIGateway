# TokenShield Request Profiler

The **Request Profiler** extracts structural metadata from incoming gateway completion payloads, producing a request profile that drives security policies, cost estimates, and tier routing.

---

## 1. Request Profile Schema
The profiler evaluates each request context and populates:
- **TaskType**: Inferred classification of the request activity.
- **RiskLevel**: Inferred risk parameters (defaulting to `medium`).
- **InputTokens** / **EstimatedOutputTokens**: Payload size parameters.
- **ContainsPii**: Identifies if sensitive identifiers exist in plain prompt text.
- **ComplexityScore**: Numeric rating from 20 to 100 indicating task difficulty.
- **RequiresReasoning** / **RequiresStructuredOutput**: Signals extracted from payload metadata keys.
- **Department** / **Environment**: Context labels mapped from header signals.

---

## 2. Inferences & Triggers

### 2.1 Task Type Keyword Matching
If `taskType` is omitted from request metadata, the profiler scans prompt message contents for text keywords:
- **`summarization`**: Matches keywords: `"summarize"`, `"summary"`, `"outline"`.
- **`translation`**: Matches keywords: `"translate"`, `"translation"`, `"language"`.
- **`coding`**: Matches keywords: `"code"`, `"programming"`, `"function"`, `"develop"`.
- **`complex_reasoning`**: Matches keywords: `"reason"`, `"think"`, `"complex"`, `"logic"`, `"analyze"`.
- **`general`**: Default task type if no matches are found.

### 2.2 PII Scanning Regexes
The profiler scans all messages text for email and telephone-like patterns:
- **Emails**: `[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}`
- **Phone Numbers**: `(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}`

If any pattern matches, `ContainsPii` is set to `true`.

---

## 3. Complexity Score Calculation
The complexity score is calculated dynamically based on payload sizes and metadata tags:
- **Base Score**: `20`
- **Large Payloads**: `+20` if input token estimate exceeds `4000` tokens.
- **Advanced Reasoning**: `+30` if `requiresReasoning` metadata is explicitly set to `true`.
- **Complex Task**: `+20` if `TaskType` is inferred or set as `"complex_reasoning"`.
- **Capping Constraint**: The final complexity score is capped at a maximum of `100`.
