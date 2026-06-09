using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
public abstract class AdminControllerBase : ControllerBase
{
    private Guid? _tenantId;

    /// <summary>
    /// Resolves the current Tenant ID.
    /// In MVP mode, it looks for the 'x-tenant-id' header. If missing, it defaults to the
    /// first Tenant found in the database (e.g. Acme Enterprise).
    /// </summary>
    protected async Task<Guid> GetTenantIdAsync(TokenShieldDbContext context)
    {
        if (_tenantId.HasValue)
        {
            return _tenantId.Value;
        }

        // 1. Resolve from header
        if (Request.Headers.TryGetValue("x-tenant-id", out var tenantIdVal) && Guid.TryParse(tenantIdVal, out var tid))
        {
            _tenantId = tid;
            return tid;
        }

        // PLACEHOLDER: Future Microsoft Entra ID claims integration
        // In a production environment, this would be read from claims:
        // var claimTid = User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
        // if (Guid.TryParse(claimTid, out var parsedTid)) { _tenantId = parsedTid; return parsedTid; }

        // 2. Local/Dev fallback to seeded demo tenant
        var defaultTenant = await context.Tenants.FirstOrDefaultAsync();
        if (defaultTenant != null)
        {
            _tenantId = defaultTenant.Id;
            return defaultTenant.Id;
        }

        throw new InvalidOperationException("Tenant context could not be resolved. Ensure database seeding has run.");
    }

    /// <summary>
    /// Resolves the Actor Email for audit log tracking.
    /// Looks for the 'x-user-email' header, falling back to a default value.
    /// </summary>
    protected string GetActorEmail()
    {
        if (Request.Headers.TryGetValue("x-user-email", out var emailVal) && !string.IsNullOrEmpty(emailVal))
        {
            return emailVal.ToString();
        }

        // PLACEHOLDER: Future Microsoft Entra ID claims integration
        // var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        // if (!string.IsNullOrEmpty(emailClaim)) return emailClaim;

        return "admin@tokenshield.local";
    }
}
