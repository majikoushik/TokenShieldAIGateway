using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TokenShield.Application.Services;
using TokenShield.Domain.Entities;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers;

[ApiController]
[Route("api/dev/api-keys")]
public class DevApiKeysController : ControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly ApiKeyService _apiKeyService;
    private readonly IWebHostEnvironment _env;

    public DevApiKeysController(TokenShieldDbContext context, ApiKeyService apiKeyService, IWebHostEnvironment env)
    {
        _context = context;
        _apiKeyService = apiKeyService;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        // Check if Tenant exists
        var tenantExists = await _context.Tenants.AnyAsync(t => t.Id == request.TenantId);
        if (!tenantExists)
        {
            return BadRequest(new { error = "Tenant not found" });
        }

        // Check if Application exists
        var appExists = await _context.ClientApplications.AnyAsync(a => a.Id == request.ClientApplicationId && a.TenantId == request.TenantId);
        if (!appExists)
        {
            return BadRequest(new { error = "Application not found or does not belong to specified Tenant" });
        }

        var prefix = request.Prefix ?? "ts_dev_";
        var (rawKey, keyHash) = _apiKeyService.GenerateKey(prefix);

        var apiKey = new ApiKey
        {
            TenantId = request.TenantId,
            ClientApplicationId = request.ClientApplicationId,
            Name = request.Name ?? "Generated Dev Key",
            Prefix = prefix,
            KeyHash = keyHash,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1),
            IsRevoked = false
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Audit the key creation
        _context.AuditLogs.Add(new AuditLog
        {
            TenantId = request.TenantId,
            ActionName = "CreateApiKey",
            EntityName = "ApiKey",
            EntityId = apiKey.Id,
            ActorEmail = "dev-environment@tokenshield.local",
            DetailsJson = $"{{\"keyName\":\"{apiKey.Name}\",\"prefix\":\"{apiKey.Prefix}\"}}"
        });
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = apiKey.Id,
            name = apiKey.Name,
            prefix = apiKey.Prefix,
            rawKey = rawKey, // Raw key shown ONLY once here
            expiresAtUtc = apiKey.ExpiresAtUtc
        });
    }

    [HttpGet]
    public async Task<IActionResult> ListKeys()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var keys = await _context.ApiKeys
            .Select(k => new
            {
                id = k.Id,
                tenantId = k.TenantId,
                clientApplicationId = k.ClientApplicationId,
                name = k.Name,
                prefix = k.Prefix,
                lastUsedAtUtc = k.LastUsedAtUtc,
                expiresAtUtc = k.ExpiresAtUtc,
                isRevoked = k.IsRevoked,
                createdAtUtc = k.CreatedAtUtc
            })
            .ToListAsync();

        return Ok(keys);
    }
}

public class CreateKeyRequest
{
    public Guid TenantId { get; set; }
    public Guid ClientApplicationId { get; set; }
    public string? Name { get; set; }
    public string? Prefix { get; set; }
}
