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

public class SettingsController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public SettingsController(TokenShieldDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet("catalog")]
    public IActionResult GetCatalog()
    {
        var tiers = Enum.GetNames(typeof(ModelTier)).Select(t => t.ToLowerInvariant()).ToList();
        var budgetScopes = Enum.GetNames(typeof(BudgetScope)).Select(s => s.ToLowerInvariant()).ToList();
        var budgetActions = Enum.GetNames(typeof(BudgetActionType)).Select(a => a.ToLowerInvariant()).ToList();
        var routingActions = Enum.GetNames(typeof(RoutingActionType)).Select(a => a.ToLowerInvariant()).ToList();

        return Ok(new
        {
            tiers,
            budgetScopes,
            budgetActions,
            routingActions
        });
    }

    [HttpGet("applications")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetApplications()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var apps = await _context.ClientApplications
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Name)
            .Select(a => new ApplicationResponse
            {
                Id = a.Id,
                Name = a.Name,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(apps);
    }

    [HttpPost("applications")]
    public async Task<ActionResult<ApplicationResponse>> CreateApplication([FromBody] CreateApplicationRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateApplicationRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var app = new ClientApplication
        {
            TenantId = tenantId,
            Name = request.Name
        };

        _context.ClientApplications.Add(app);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateClientApplication",
            "ClientApplication",
            app.Id,
            GetActorEmail(),
            new { name = app.Name });

        var response = new ApplicationResponse
        {
            Id = app.Id,
            Name = app.Name,
            CreatedAtUtc = app.CreatedAtUtc
        };

        return Ok(response);
    }
}
