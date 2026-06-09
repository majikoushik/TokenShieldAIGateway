using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;

namespace TokenShield.CostEngine.Services;

public class CostEngineService : ICostEngineService
{
    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // MVP Rule: 1 token ≈ 4 characters
        return (int)Math.Ceiling(text.Length / 4.0);
    }

    public int EstimateRequestTokens(ChatCompletionRequest request)
    {
        if (request?.Messages == null || request.Messages.Count == 0)
        {
            return 0;
        }

        var totalChars = 0;
        foreach (var msg in request.Messages)
        {
            totalChars += (msg.Role?.Length ?? 0);
            totalChars += (msg.Content?.Length ?? 0);
        }

        return (int)Math.Ceiling(totalChars / 4.0);
    }

    public decimal CalculateCost(int tokens, decimal pricePerMillion)
    {
        if (tokens <= 0 || pricePerMillion <= 0)
        {
            return 0m;
        }

        // Estimated cost formula using decimal arithmetic
        return (tokens / 1000000.0m) * pricePerMillion;
    }
}
