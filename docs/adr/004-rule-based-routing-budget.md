# ADR 004: Rule-Based Routing and Budget Enforcement Before Execution

## Status
Accepted

## Context
One of TokenShield's primary goals is to prevent cost overruns and enforce governance. Checking budgets or applying rules *after* the provider is called defeats the purpose of an AI gateway for cost-control.

## Decision
We perform a Pre-Call Budget Check and Rule Evaluation. Token estimation runs on the incoming request to calculate the potential cost, the rule engine matches the request profile against tenant rules to pick the right tier, and the budget engine ensures limits are not exceeded *before* making the external HTTP call.

## Consequences
- **Pros**: Absolutely guarantees hard limits are not exceeded. Downgrade policies and blocking rules take effect immediately.
- **Cons**: Introduces slight latency on every request due to rule evaluation and DB lookups (mitigated by caching and fast token estimation). Token estimation might slightly differ from the actual provider token count, meaning the final cost requires a post-call reconciliation.

## Alternatives Considered
- **Post-call asynchronous budget updates**: Would allow requests to go through faster but could lead to massive budget overruns during a burst of requests before the budget is updated.
