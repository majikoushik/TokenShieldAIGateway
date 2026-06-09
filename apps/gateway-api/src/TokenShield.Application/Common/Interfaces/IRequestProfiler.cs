using TokenShield.Domain.Models;
using TokenShield.Application.Dto;

namespace TokenShield.Application.Common.Interfaces;

public interface IRequestProfiler
{
    Task<RequestProfile> ProfileRequestAsync(ChatCompletionRequest request, int inputTokens, CancellationToken cancellationToken = default);
}
