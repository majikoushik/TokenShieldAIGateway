using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto.Admin;
using TokenShield.Application.Validators.Admin;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class BudgetsController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public BudgetsController(TokenShieldDbContext context, IAuditLoggingService _auditLoggingService)
    {
        this._context = context;
        this._auditLoggingService = _auditLoggingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BudgetResponse>>> GetBudgets()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var budgets = await _context.BudgetLimits
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.Scope).ThenBy(b => b.MonthlyLimit)
            .ToListAsync();

        var response = new List<BudgetResponse>();
        foreach (var b in budgets)
        {
            response.Add(new BudgetResponse
            {
                Id = b.Id,
                Scope = b.Scope,
                TargetId = b.TargetId,
                TargetName = await GetTargetNameAsync(b.Scope, b.TargetId),
                MonthlyLimit = b.MonthlyLimit,
                WarningThresholdPercent = b.WarningThresholdPercent,
                CurrentSpend = b.CurrentSpend,
                Action = b.Action,
                LastResetAtUtc = b.LastResetAtUtc,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc
            });
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BudgetResponse>> GetBudget(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var b = await _context.BudgetLimits
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (b == null)
        {
            return NotFound(new { error = "Budget limit not found." });
        }

        var response = new BudgetResponse
        {
            Id = b.Id,
            Scope = b.Scope,
            TargetId = b.TargetId,
            TargetName = await GetTargetNameAsync(b.Scope, b.TargetId),
            MonthlyLimit = b.MonthlyLimit,
            WarningThresholdPercent = b.WarningThresholdPercent,
            CurrentSpend = b.CurrentSpend,
            Action = b.Action,
            LastResetAtUtc = b.LastResetAtUtc,
            CreatedAtUtc = b.CreatedAtUtc,
            UpdatedAtUtc = b.UpdatedAtUtc
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<BudgetResponse>> CreateBudget([FromBody] CreateBudgetRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateBudgetRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Validate scope and target existence
        if (request.Scope == BudgetScope.Tenant)
        {
            if (request.TargetId != null)
            {
                return BadRequest(new { error = "TargetId must be null for Tenant scope budget." });
            }
        }
        else if (request.Scope == BudgetScope.Application)
        {
            if (request.TargetId == null) return BadRequest(new { error = "TargetId is required for Application scope." });
            var exists = await _context.ClientApplications.AnyAsync(x => x.Id == request.TargetId && x.TenantId == tenantId);
            if (!exists) return BadRequest(new { error = "Application not found or does not belong to your tenant." });
        }
        else if (request.Scope == BudgetScope.ApiKey)
        {
            if (request.TargetId == null) return BadRequest(new { error = "TargetId is required for ApiKey scope." });
            var exists = await _context.ApiKeys.AnyAsync(x => x.Id == request.TargetId && x.TenantId == tenantId);
            if (!exists) return BadRequest(new { error = "API key not found or does not belong to your tenant." });
        }
        else if (request.Scope == BudgetScope.Model)
        {
            if (request.TargetId == null) return BadRequest(new { error = "TargetId is required for Model scope." });
            var exists = await _context.AiModels.Include(m => m.Provider).AnyAsync(x => x.Id == request.TargetId && x.Provider.TenantId == tenantId);
            if (!exists) return BadRequest(new { error = "Model not found or does not belong to your tenant." });
        }

        var b = new BudgetLimit
        {
            TenantId = tenantId,
            Scope = request.Scope,
            TargetId = request.TargetId,
            MonthlyLimit = request.MonthlyLimit,
            WarningThresholdPercent = request.WarningThresholdPercent,
            CurrentSpend = 0m,
            LastResetAtUtc = DateTime.UtcNow,
            Action = request.Action
        };

        _context.BudgetLimits.Add(b);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateBudget",
            "BudgetLimit",
            b.Id,
            GetActorEmail(),
            new { scope = b.Scope, targetId = b.TargetId, monthlyLimit = b.MonthlyLimit, warningThreshold = b.WarningThresholdPercent, action = b.Action });

        var response = new BudgetResponse
        {
            Id = b.Id,
            Scope = b.Scope,
            TargetId = b.TargetId,
            TargetName = await GetTargetNameAsync(b.Scope, b.TargetId),
            MonthlyLimit = b.MonthlyLimit,
            WarningThresholdPercent = b.WarningThresholdPercent,
            CurrentSpend = b.CurrentSpend,
            Action = b.Action,
            LastResetAtUtc = b.LastResetAtUtc,
            CreatedAtUtc = b.CreatedAtUtc,
            UpdatedAtUtc = b.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetBudget), new { id = b.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new UpdateBudgetRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var b = await _context.BudgetLimits
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (b == null)
        {
            return NotFound(new { error = "Budget limit not found." });
        }

        b.MonthlyLimit = request.MonthlyLimit;
        b.WarningThresholdPercent = request.WarningThresholdPercent;
        b.Action = request.Action;
        b.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "UpdateBudget",
            "BudgetLimit",
            b.Id,
            GetActorEmail(),
            new { monthlyLimit = b.MonthlyLimit, warningThreshold = b.WarningThresholdPercent, action = b.Action });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var b = await _context.BudgetLimits
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (b == null)
        {
            return NotFound(new { error = "Budget limit not found." });
        }

        b.IsDeleted = true;
        b.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteBudget",
            "BudgetLimit",
            b.Id,
            GetActorEmail(),
            new { scope = b.Scope, targetId = b.TargetId });

        return NoContent();
    }

    private async Task<string?> GetTargetNameAsync(BudgetScope scope, Guid? targetId)
    {
        if (targetId == null) return null;
        return scope switch
        {
            BudgetScope.Application => await _context.ClientApplications.Where(x => x.Id == targetId).Select(x => x.Name).FirstOrDefaultAsync(),
            BudgetScope.ApiKey => await _context.ApiKeys.Where(x => x.Id == targetId).Select(x => x.Name).FirstOrDefaultAsync(),
            BudgetScope.Model => await _context.AiModels.Where(x => x.Id == targetId).Select(x => x.Name).FirstOrDefaultAsync(),
            _ => null
        };
    }
}
