using TokenShield.Application.Dto;

namespace TokenShield.Application.Common.Interfaces;

public interface ICostEngineService
{
    int EstimateTokens(string text);
    int EstimateRequestTokens(ChatCompletionRequest request);
    decimal CalculateCost(int tokens, decimal pricePerMillion);
}
