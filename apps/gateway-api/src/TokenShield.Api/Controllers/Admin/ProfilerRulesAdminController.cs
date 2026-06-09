using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto.Admin;
using TokenShield.Application.Validators.Admin;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class ProfilerRulesAdminController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly IProfilerRuleService _profilerRuleService;

    public ProfilerRulesAdminController(
        TokenShieldDbContext context, 
        IAuditLoggingService auditLoggingService,
        IProfilerRuleService profilerRuleService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
        _profilerRuleService = profilerRuleService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProfilerRuleDto>>> GetProfilerRules()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var dbRules = await _context.ProfilerRules
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        var dtos = dbRules.Select(r => new ProfilerRuleDto
        {
            Id = r.Id,
            Name = r.Name,
            TargetTaskType = r.TargetTaskType,
            Phrases = ParseJsonArray(r.PhrasesJson),
            RegexPatterns = ParseJsonArray(r.RegexPatternsJson),
            Confidence = r.Confidence,
            Priority = r.Priority,
            IsActive = r.IsActive,
            CreatedAtUtc = r.CreatedAtUtc,
            UpdatedAtUtc = r.UpdatedAtUtc
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfilerRuleDto>> GetProfilerRule(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var rule = await _context.ProfilerRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null) return NotFound(new { error = "Profiler rule not found." });

        return Ok(new ProfilerRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            TargetTaskType = rule.TargetTaskType,
            Phrases = ParseJsonArray(rule.PhrasesJson),
            RegexPatterns = ParseJsonArray(rule.RegexPatternsJson),
            Confidence = rule.Confidence,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc
        });
    }

    [HttpPost]
    public async Task<ActionResult<ProfilerRuleDto>> CreateProfilerRule([FromBody] CreateProfilerRuleRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateProfilerRuleRequestValidator();
        var valResult = await validator.ValidateAsync(request);
        if (!valResult.IsValid) return BadRequest(new { errors = valResult.Errors.Select(e => e.ErrorMessage) });

        var rule = new ProfilerRule
        {
            TenantId = tenantId,
            Name = request.Name,
            TargetTaskType = request.TargetTaskType,
            PhrasesJson = JsonSerializer.Serialize(request.Phrases ?? new List<string>()),
            RegexPatternsJson = JsonSerializer.Serialize(request.RegexPatterns ?? new List<string>()),
            Confidence = request.Confidence,
            Priority = request.Priority,
            IsActive = request.IsActive
        };

        _context.ProfilerRules.Add(rule);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateProfilerRule",
            "ProfilerRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name, targetTaskType = rule.TargetTaskType, isActive = rule.IsActive });

        var dto = new ProfilerRuleDto
        {
            Id = rule.Id,
            Name = rule.Name,
            TargetTaskType = rule.TargetTaskType,
            Phrases = ParseJsonArray(rule.PhrasesJson),
            RegexPatterns = ParseJsonArray(rule.RegexPatternsJson),
            Confidence = rule.Confidence,
            Priority = rule.Priority,
            IsActive = rule.IsActive,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetProfilerRule), new { id = rule.Id }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProfilerRule(Guid id, [FromBody] UpdateProfilerRuleRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new UpdateProfilerRuleRequestValidator();
        var valResult = await validator.ValidateAsync(request);
        if (!valResult.IsValid) return BadRequest(new { errors = valResult.Errors.Select(e => e.ErrorMessage) });

        var rule = await _context.ProfilerRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null) return NotFound(new { error = "Profiler rule not found." });

        rule.Name = request.Name;
        rule.TargetTaskType = request.TargetTaskType;
        rule.PhrasesJson = JsonSerializer.Serialize(request.Phrases ?? new List<string>());
        rule.RegexPatternsJson = JsonSerializer.Serialize(request.RegexPatterns ?? new List<string>());
        rule.Confidence = request.Confidence;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "UpdateProfilerRule",
            "ProfilerRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name, targetTaskType = rule.TargetTaskType, isActive = rule.IsActive });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProfilerRule(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var rule = await _context.ProfilerRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId);

        if (rule == null) return NotFound(new { error = "Profiler rule not found." });

        rule.IsDeleted = true;
        rule.IsActive = false;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteProfilerRule",
            "ProfilerRule",
            rule.Id,
            GetActorEmail(),
            new { name = rule.Name });

        return NoContent();
    }

    [HttpPost("test")]
    public async Task<ActionResult<TestProfilerRuleResponse>> TestProfilerRule([FromBody] TestProfilerRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(new { error = "Prompt is required for testing." });
        }

        var result = await _profilerRuleService.TestRuleAsync(request);
        return Ok(result);
    }

    private List<string> ParseJsonArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
