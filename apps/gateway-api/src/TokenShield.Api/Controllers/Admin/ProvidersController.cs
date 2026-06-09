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

public class ProvidersController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public ProvidersController(TokenShieldDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProviderResponse>>> GetProviders()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var providers = await _context.ModelProviders
            .Where(p => p.TenantId == tenantId)
            .OrderBy(p => p.Name)
            .Select(p => new ProviderResponse
            {
                Id = p.Id,
                Name = p.Name,
                ApiUrl = p.ApiUrl,
                ApiKeySecretRef = p.ApiKeySecretRef,
                IsActive = p.IsActive,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(providers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProviderResponse>> GetProvider(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var provider = await _context.ModelProviders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (provider == null)
        {
            return NotFound(new { error = "Provider not found." });
        }

        var response = new ProviderResponse
        {
            Id = provider.Id,
            Name = provider.Name,
            ApiUrl = provider.ApiUrl,
            ApiKeySecretRef = provider.ApiKeySecretRef,
            IsActive = provider.IsActive,
            CreatedAtUtc = provider.CreatedAtUtc,
            UpdatedAtUtc = provider.UpdatedAtUtc
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ProviderResponse>> CreateProvider([FromBody] CreateProviderRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateProviderRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var provider = new ModelProvider
        {
            TenantId = tenantId,
            Name = request.Name,
            ApiUrl = request.ApiUrl,
            ApiKeySecretRef = request.ApiKeySecretRef,
            IsActive = request.IsActive
        };

        _context.ModelProviders.Add(provider);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateProvider",
            "ModelProvider",
            provider.Id,
            GetActorEmail(),
            new { name = provider.Name, apiUrl = provider.ApiUrl, apiKeySecretRef = provider.ApiKeySecretRef, isActive = provider.IsActive });

        var response = new ProviderResponse
        {
            Id = provider.Id,
            Name = provider.Name,
            ApiUrl = provider.ApiUrl,
            ApiKeySecretRef = provider.ApiKeySecretRef,
            IsActive = provider.IsActive,
            CreatedAtUtc = provider.CreatedAtUtc,
            UpdatedAtUtc = provider.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] UpdateProviderRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new UpdateProviderRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var provider = await _context.ModelProviders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (provider == null)
        {
            return NotFound(new { error = "Provider not found." });
        }

        provider.Name = request.Name;
        provider.ApiUrl = request.ApiUrl;
        provider.ApiKeySecretRef = request.ApiKeySecretRef;
        provider.IsActive = request.IsActive;
        provider.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "UpdateProvider",
            "ModelProvider",
            provider.Id,
            GetActorEmail(),
            new { name = provider.Name, apiUrl = provider.ApiUrl, apiKeySecretRef = provider.ApiKeySecretRef, isActive = provider.IsActive });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProvider(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var provider = await _context.ModelProviders
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (provider == null)
        {
            return NotFound(new { error = "Provider not found." });
        }

        provider.IsDeleted = true;
        provider.IsActive = false;
        provider.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteProvider",
            "ModelProvider",
            provider.Id,
            GetActorEmail(),
            new { name = provider.Name });

        return NoContent();
    }
}
