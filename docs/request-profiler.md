# TokenShield Request Profiler

The **Request Profiler** extracts structural metadata from incoming gateway completion payloads, producing a request profile that drives security policies, cost estimates, and tier routing.

## 1. Request Profile Architecture

The request profiler has been upgraded to a **Production-Grade** configurable architecture that orchestrates multiple signal classifiers to form a cohesive confidence-based profile.

The profiling pipeline consists of modular components:
1. **Metadata Resolver**: Pulls predefined explicit signals directly from request metadata.
2. **Task Classifiers**: Evaluates prompt text using configurable keyword/regex rules. 
3. **Risk Classifiers**: Infers risk based on identified signals.
4. **Data Sensitivity Detector**: Scans prompts for PII, financial, health, and legal information.
5. **Complexity Scorer**: Generates an overall complexity metric driven by token size and semantic difficulty.
6. **Profile Result Merger**: Consolidates results into a finalized confidence-based request profile.

The system supports multiple modes via the `IRequestProfilerFactory`:
- `Mvp`: The legacy keyword-driven profiler.
- `Production`: The new orchestration-based multi-classifier profiler.
- `Hybrid`: Invokes `Production`, falling back to `Mvp` if task confidence falls below a configured threshold.

---

## 2. Request Profile Schema
The profiler populates the internal profile with:
- **TaskType**: Inferred classification of the request activity.
- **TaskTypeConfidence**: 0.0-1.0 float representing confidence of the classification.
- **RiskLevel**: Inferred risk parameters (`low`, `medium`, `high`, `critical`).
- **RiskConfidence**: Confidence score for risk classification.
- **InputTokens** / **EstimatedOutputTokens**: Payload size parameters.
- **ContainsPii** / **ContainsFinancialData** / **ContainsHealthData** / **ContainsLegalData**: Identifies sensitive identifiers.
- **DataSensitivity**: Overall category of sensitive data (`pii_only`, `financial`, `health_critical`, etc.).
- **ComplexityScore**: Numeric rating (0 to 100) indicating task difficulty.
- **ComplexityBand**: String categorization (`low`, `medium`, `high`, `extreme`).
- **RequiresReasoning** / **RequiresStructuredOutput**: Signals extracted from payload.
- **Department** / **Environment**: Context labels mapped from header signals.

---

## 3. Configuration & Rules

Configuration resides in `appsettings.json` under the `RequestProfiler` block:

```json
"RequestProfiler": {
  "Mode": "Hybrid",
  "EnableProductionProfiler": true,
  "LowConfidenceThreshold": 0.6,
  "HighConfidenceThreshold": 0.85,
  "TaskClassificationRules": [
    {
      "TaskType": "summarization",
      "Phrases": ["summarize", "tl;dr", "key points"],
      "Confidence": 0.82,
      "Priority": 10
    }
  ]
}
```

### 3.1 Task Classification Rules
Task types are evaluated asynchronously via `ConfigurableRuleBasedTaskClassifier`. Each rule defines:
- A `TaskType` target label.
- A list of matching exact/partial text `Phrases`.
- Optional list of `RegexPatterns`.
- A defined `Confidence` multiplier and conflict `Priority`.

If no tasks match, the profiler falls back to the configured `DefaultTaskType` (defaulting to `"general"`).

### 3.2 Data Sensitivity Scanning
The `RegexSensitivityDetector` scans all prompt messages for common text patterns:
- **Emails / PII**: `[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}`
- **Phone Numbers**: `(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}`
- **Credit Cards / Financial**: `\b(?:\d[ -]*?){13,16}\b`
- **SSN / Legal**: `\b\d{3}-\d{2}-\d{4}\b`

If matches are detected, specific bit flags are enabled, and an aggregated `DataSensitivity` label is constructed.

### 3.3 Complexity Scoring
Complexity is evaluated through the `DefaultComplexityScorer`, utilizing:
- **Base Score**: 10 points.
- **Payload Multipliers**: Points assigned relative to input tokens. (e.g. `+15` for > 1000 tokens, `+30` for > 4000).
- **Task Specific Additions**: E.g. `+20` for complex reasoning tasks.
- **Risk Multipliers**: `+10` for high risk scenarios.
- **Final Banding**: The aggregate score maps to `ComplexityBand` outputs like `low` (<30), `medium`, or `extreme` (>80).
