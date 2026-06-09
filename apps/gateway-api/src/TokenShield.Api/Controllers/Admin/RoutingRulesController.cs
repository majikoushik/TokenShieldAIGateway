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
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class RoutingRulesController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public RoutingRulesController(TokenShieldDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoutingRuleResponse>>> GetRoutingRules()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var rules = await _context.RoutingRules
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Priority)
            .Select(r => new RoutingRuleResponse
            {
                Id = r.Id,
                Name = r.Name,
                Priority = r.Priority,
                ConditionsJson = r.ConditionsJson,
                Action = r.Action,
                TargetTier = r.TargetTier,
                IsActive = r.IsActive,
                CreatedAtUtc = r.CreatedAtUtc,
                UpdatedAtUtc = r.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(rules);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoutingRuleResponse>> GetRoutingRule(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var rule = await _context.RoutingRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null)
        {
            return NotFound(new { error = "Routing rule not found." });
        }

        var response = new RoutingRuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            Priority = rule.Priority,
            ConditionsJson = rule.ConditionsJson,
            Action = rule.Action,
            TargetTier = rule.TargetTier,
            IsActive = rule.IsActive,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<RoutingRuleResponse>> CreateRoutingRule([FromBody] CreateRoutingRuleRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateRoutingRuleRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Validate JSON payload
        try
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(request.ConditionsJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { error = "ConditionsJson contains invalid JSON structure." });
        }

        var rule = new RoutingRule
        {
            TenantId = tenantId,
            Name = request.Name,
            Priority = request.Priority,
            ConditionsJson = request.ConditionsJson,
            Action = request.Action,
            TargetTier = request.TargetTier,
            IsActive = request.IsActive
        };

        _context.RoutingRules.Add(rule);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateRoutingRule",
            "RoutingRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name, priority = rule.Priority, conditions = rule.ConditionsJson, action = rule.Action, targetTier = rule.TargetTier, isActive = rule.IsActive });

        var response = new RoutingRuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            Priority = rule.Priority,
            ConditionsJson = rule.ConditionsJson,
            Action = rule.Action,
            TargetTier = rule.TargetTier,
            IsActive = rule.IsActive,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetRoutingRule), new { id = rule.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoutingRule(Guid id, [FromBody] UpdateRoutingRuleRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new UpdateRoutingRuleRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Validate JSON payload
        try
        {
            using var jsonDoc = System.Text.Json.JsonDocument.Parse(request.ConditionsJson);
        }
        catch (System.Text.Json.JsonException)
        {
            return BadRequest(new { error = "ConditionsJson contains invalid JSON structure." });
        }

        var rule = await _context.RoutingRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null)
        {
            return NotFound(new { error = "Routing rule not found." });
        }

        rule.Name = request.Name;
        rule.Priority = request.Priority;
        rule.ConditionsJson = request.ConditionsJson;
        rule.Action = request.Action;
        rule.TargetTier = request.TargetTier;
        rule.IsActive = request.IsActive;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "UpdateRoutingRule",
            "RoutingRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name, priority = rule.Priority, conditions = rule.ConditionsJson, action = rule.Action, targetTier = rule.TargetTier, isActive = rule.IsActive });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoutingRule(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var rule = await _context.RoutingRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null)
        {
            return NotFound(new { error = "Routing rule not found." });
        }

        rule.IsDeleted = true;
        rule.IsActive = false;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteRoutingRule",
            "RoutingRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name });

        return NoContent();
    }
}
