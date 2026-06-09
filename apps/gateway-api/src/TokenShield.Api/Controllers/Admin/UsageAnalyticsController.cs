using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Dto.Admin;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class UsageAnalyticsController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;

    public UsageAnalyticsController(TokenShieldDbContext context)
    {
        _context = context;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<IEnumerable<UsageLogResponse>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? applicationId = null,
        [FromQuery] string? provider = null,
        [FromQuery] string? model = null,
        [FromQuery] string? tier = null,
        [FromQuery] string? status = null,
        [FromQuery] string? budgetStatus = null)
    {
        var tenantId = await GetTenantIdAsync(_context);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.AiRequestLogs
            .Where(l => l.TenantId == tenantId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc >= startDate.Value.ToUniversalTime());
        }
        if (endDate.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc <= endDate.Value.ToUniversalTime());
        }
        if (applicationId.HasValue)
        {
            query = query.Where(l => l.ApplicationId == applicationId.Value);
        }
        if (!string.IsNullOrEmpty(provider))
        {
            query = query.Where(l => EF.Functions.ILike(l.SelectedProvider, $"%{provider}%"));
        }
        if (!string.IsNullOrEmpty(model))
        {
            query = query.Where(l => EF.Functions.ILike(l.SelectedModel, $"%{model}%"));
        }
        if (!string.IsNullOrEmpty(tier))
        {
            query = query.Where(l => EF.Functions.ILike(l.SelectedTier, $"%{tier}%"));
        }
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(l => EF.Functions.ILike(l.RequestStatus, $"%{status}%"));
        }
        if (!string.IsNullOrEmpty(budgetStatus))
        {
            query = query.Where(l => EF.Functions.ILike(l.BudgetStatus, $"%{budgetStatus}%"));
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load applications list under tenant to map names fast
        var appNames = await _context.ClientApplications
            .Where(a => a.TenantId == tenantId)
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        var response = logs.Select(l => new UsageLogResponse
        {
            Id = l.Id,
            CorrelationId = l.CorrelationId,
            RequestId = l.RequestId,
            ApplicationId = l.ApplicationId,
            ApplicationName = appNames.TryGetValue(l.ApplicationId, out var name) ? name : "Unknown Application",
            PromptHash = l.PromptHash,
            ResponseHash = l.ResponseHash,
            InputTokens = l.InputTokens,
            OutputTokens = l.OutputTokens,
            EstimatedCost = l.EstimatedCost,
            SelectedProvider = l.SelectedProvider,
            SelectedModel = l.SelectedModel,
            SelectedTier = l.SelectedTier,
            MatchedRuleName = l.MatchedRuleName,
            FallbackUsed = l.FallbackUsed,
            BudgetStatus = l.BudgetStatus,
            RequestStatus = l.RequestStatus,
            LatencyMs = l.LatencyMs,
            CreatedAtUtc = l.CreatedAtUtc
        }).ToList();

        Response.Headers["X-Total-Count"] = totalItems.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();

        return Ok(response);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var query = _context.AiRequestLogs
            .Where(l => l.TenantId == tenantId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc >= startDate.Value.ToUniversalTime());
        }
        if (endDate.HasValue)
        {
            query = query.Where(l => l.CreatedAtUtc <= endDate.Value.ToUniversalTime());
        }

        var logs = await query.ToListAsync();

        if (logs.Count == 0)
        {
            return Ok(new DashboardSummaryResponse());
        }

        var totalCost = logs.Sum(l => l.EstimatedCost);
        var totalRequests = logs.Count;
        var totalInputTokens = logs.Sum(l => l.InputTokens);
        var totalOutputTokens = logs.Sum(l => l.OutputTokens);
        var averageLatency = logs.Average(l => l.LatencyMs);

        // Aggregations
        var costByProvider = logs
            .GroupBy(l => l.SelectedProvider)
            .Select(g => new MetricStats
            {
                GroupKey = g.Key,
                Cost = g.Sum(l => l.EstimatedCost),
                RequestCount = g.Count()
            })
            .OrderByDescending(x => x.Cost)
            .ToList();

        var costByModel = logs
            .GroupBy(l => l.SelectedModel)
            .Select(g => new MetricStats
            {
                GroupKey = g.Key,
                Cost = g.Sum(l => l.EstimatedCost),
                RequestCount = g.Count()
            })
            .OrderByDescending(x => x.Cost)
            .ToList();

        var costByTier = logs
            .GroupBy(l => l.SelectedTier)
            .Select(g => new MetricStats
            {
                GroupKey = g.Key,
                Cost = g.Sum(l => l.EstimatedCost),
                RequestCount = g.Count()
            })
            .OrderByDescending(x => x.Cost)
            .ToList();

        var requestsByStatus = logs
            .GroupBy(l => l.RequestStatus)
            .Select(g => new CountStat
            {
                GroupKey = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var requestsByBudgetState = logs
            .GroupBy(l => l.BudgetStatus)
            .Select(g => new CountStat
            {
                GroupKey = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var response = new DashboardSummaryResponse
        {
            TotalCost = totalCost,
            TotalRequests = totalRequests,
            TotalInputTokens = totalInputTokens,
            TotalOutputTokens = totalOutputTokens,
            AverageLatencyMs = averageLatency,
            CostByProvider = costByProvider,
            CostByModel = costByModel,
            CostByTier = costByTier,
            RequestByStatus = requestsByStatus,
            RequestByBudgetState = requestsByBudgetState
        };

        return Ok(response);
    }
}
