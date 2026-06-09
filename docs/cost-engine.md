# TokenShield Cost Engine

The **Cost Engine** handles token volume estimation and monetary pricing calculations across the gateway proxy pipeline.

---

## 1. Token Estimation Formula
To maintain high performance and low overhead during the request lifecycle, the cost engine applies a lightweight character-based approximation:
- **Approximation Constraint**: $1 \text{ token} \approx 4 \text{ characters}$.
- **Formula**:
  $$\text{Tokens} = \lceil \frac{\text{Total Characters}}{4} \rceil$$
- **Request Estimations**: Computes total character size across the roles and message contents of all items in the request `messages` array.
- **Response Estimations**: Computes character size of the generated completion text returned from the model provider.

---

## 2. Cost Calculations
Cost calculations utilize precise C# `decimal` parameters (never float or double) to prevent floating-point rounding errors:
- **Pricing Units**: Database model tables store input and output token pricing per one million tokens (`InputTokenPricePerMillion`, `OutputTokenPricePerMillion`).
- **Cost Formula**:
  $$\text{Input Cost} = \frac{\text{Input Tokens}}{1,000,000} \times \text{Input Price}$$
  $$\text{Output Cost} = \frac{\text{Output Tokens}}{1,000,000} \times \text{Output Price}$$
  $$\text{Total Estimated Cost} = \text{Input Cost} + \text{Output Cost}$$

---

## 3. Persistent Telemetry Logs
Once cost metrics are resolved:
- The values are returned back to client in the OpenAI-compatible response wrapper (`routing.estimatedCost`).
- Write actions record cost metrics directly inside the `AiRequestLogs` table with a database scale of `decimal(18,6)` to support granular spend reporting.
