using System;
using System.Threading;
using System.Threading.Tasks;
using TokenShield.Application.Dto;
using TokenShield.Domain.Enums;

namespace TokenShield.Application.Common.Interfaces;

public interface IProviderExecutionService
{
    Task<ProviderExecutionResult> ExecuteWithFallbackAsync(
        Guid tenantId,
        ModelTier selectedTier,
        ChatCompletionRequest request,
        CancellationToken cancellationToken);
}
