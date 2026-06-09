using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto.Admin;
using TokenShield.Application.Services;
using TokenShield.Application.Validators.Admin;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class ApiKeysController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly ApiKeyService _apiKeyService;
    private readonly IAuditLoggingService _auditLoggingService;

    public ApiKeysController(TokenShieldDbContext context, ApiKeyService apiKeyService, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiKeyResponse>>> GetApiKeys()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var keys = await _context.ApiKeys
            .Include(k => k.ClientApplication)
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyResponse
            {
                Id = k.Id,
                ClientApplicationId = k.ClientApplicationId,
                ClientApplicationName = k.ClientApplication.Name,
                Name = k.Name,
                Prefix = k.Prefix,
                LastUsedAtUtc = k.LastUsedAtUtc,
                ExpiresAtUtc = k.ExpiresAtUtc,
                IsRevoked = k.IsRevoked,
                CreatedAtUtc = k.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(keys);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiKeyResponse>> GetApiKey(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var k = await _context.ApiKeys
            .Include(k => k.ClientApplication)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (k == null)
        {
            return NotFound(new { error = "API key not found." });
        }

        var response = new ApiKeyResponse
        {
            Id = k.Id,
            ClientApplicationId = k.ClientApplicationId,
            ClientApplicationName = k.ClientApplication.Name,
            Name = k.Name,
            Prefix = k.Prefix,
            LastUsedAtUtc = k.LastUsedAtUtc,
            ExpiresAtUtc = k.ExpiresAtUtc,
            IsRevoked = k.IsRevoked,
            CreatedAtUtc = k.CreatedAtUtc
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ApiKeyCreatedResponse>> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateApiKeyRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Validate client application existence and ownership
        var app = await _context.ClientApplications
            .FirstOrDefaultAsync(x => x.Id == request.ClientApplicationId && x.TenantId == tenantId);

        if (app == null)
        {
            return BadRequest(new { error = "Invalid ClientApplicationId specified or application does not belong to your tenant." });
        }

        var prefix = request.Prefix ?? "ts_live_";
        if (prefix != "ts_live_" && prefix != "ts_dev_")
        {
            return BadRequest(new { error = "Prefix must be either 'ts_live_' or 'ts_dev_'." });
        }

        var (rawKey, keyHash) = _apiKeyService.GenerateKey(prefix);

        var apiKey = new ApiKey
        {
            TenantId = tenantId,
            ClientApplicationId = request.ClientApplicationId,
            Name = request.Name,
            Prefix = prefix,
            KeyHash = keyHash,
            ExpiresAtUtc = request.ExpiresAtUtc ?? DateTime.UtcNow.AddYears(1),
            IsRevoked = false
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateApiKey",
            "ApiKey",
            apiKey.Id,
            GetActorEmail(),
            new { name = apiKey.Name, prefix = apiKey.Prefix, expiresAtUtc = apiKey.ExpiresAtUtc });

        var response = new ApiKeyCreatedResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Prefix = apiKey.Prefix,
            RawKey = rawKey, // Raw key shown ONLY once on creation!
            ExpiresAtUtc = apiKey.ExpiresAtUtc
        };

        return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, response);
    }

    [HttpPost("{id}/revoke")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var k = await _context.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (k == null)
        {
            return NotFound(new { error = "API key not found." });
        }

        if (k.IsRevoked)
        {
            return BadRequest(new { error = "API key is already revoked." });
        }

        k.IsRevoked = true;
        k.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "RevokeApiKey",
            "ApiKey",
            k.Id,
            GetActorEmail(),
            new { name = k.Name });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiKey(Guid id)
    {
        // Deleting a key is functionally equivalent to revoking it or soft-deleting it
        var tenantId = await GetTenantIdAsync(_context);

        var k = await _context.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

        if (k == null)
        {
            return NotFound(new { error = "API key not found." });
        }

        k.IsRevoked = true;
        k.IsDeleted = true;
        k.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteApiKey",
            "ApiKey",
            k.Id,
            GetActorEmail(),
            new { name = k.Name });

        return NoContent();
    }
}
