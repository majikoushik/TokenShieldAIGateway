using TokenShield.Domain.Models;
using TokenShield.Application.Dto;

namespace TokenShield.Application.Common.Interfaces;

public interface IRequestProfiler
{
    RequestProfile ProfileRequest(ChatCompletionRequest request, int inputTokens);
}
