# TokenShield Policy Routing Rules Engine

The **Routing Rules Engine** matches request profiles against active priority conditions, selecting the routing tier or blocking actions.

---

## 1. Engine Execution Sequence
1. **Fetch Policies**: Query active `RoutingRules` matching the `TenantId` constraint.
2. **Order Rules**: Sort rules by `Priority` ascending (lower priority numbers execute first).
3. **Condition Evaluator**: Evaluate condition arrays for each rule. If all conditions match, select that rule action immediately.
4. **Default Action Fallback**: If no rules match:
   - If `RiskLevel` is `"high"`, trigger `HumanReview`.
   - Otherwise, default to the `Standard` model tier.

---

## 2. Supported Conditions Conditions Matrix

A rule holds a condition array matching the schema: `[{"field": "...", "operator": "...", "value": "..."}]`

### 2.1 Fields
- **taskType**: Evaluated against string task types (e.g. `summarization`, `coding`).
- **riskLevel**: Evaluated against string risk designations (`low`, `medium`, `high`).
- **containsPii**: Evaluated against boolean matches (`true`/`false`).
- **inputTokens**: Evaluated against integer token sizes.
- **complexityScore**: Evaluated against integer complexity ranges.
- **department** / **environment**: String comparisons.

### 2.2 Operators
- **equals** / **notEquals**: Supported for all data types.
- **greaterThan** / **lessThan** / **greaterThanOrEquals** / **lessThanOrEquals**: Evaluated on integer properties only (`inputTokens`, `complexityScore`).

---

## 3. Policy Action Outcomes

### 3.1 routeToTier
Selects a designated model tier (`cheap`, `standard`, `premium`):
- The engine queries database models matching the tier criteria.
- Resolves model configurations and returns standard completion payload.

### 3.2 block
Blocks execution immediately. Returns a safe `403 Forbidden` JSON error payload:
- **Status Code**: `403`
- **Error Type**: `policy_blocked`

### 3.3 humanReview
Places the execution in review holds. Returns a safe `422 Unprocessable Entity` JSON error payload:
- **Status Code**: `422`
- **Error Type**: `human_review_required`
