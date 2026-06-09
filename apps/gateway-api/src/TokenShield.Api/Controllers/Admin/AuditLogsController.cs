using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TokenShield.Application.Dto.Admin;
using TokenShield.Infrastructure.Persistence;

namespace TokenShield.Api.Controllers.Admin;

public class AuditLogsController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;

    public AuditLogsController(TokenShieldDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogResponse>>> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? actorEmail = null,
        [FromQuery] string? actionName = null,
        [FromQuery] string? entityName = null)
    {
        var tenantId = await GetTenantIdAsync(_context);

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _context.AuditLogs
            .Where(a => a.TenantId == tenantId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc >= startDate.Value.ToUniversalTime());
        }
        if (endDate.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc <= endDate.Value.ToUniversalTime());
        }
        if (!string.IsNullOrEmpty(actorEmail))
        {
            query = query.Where(a => EF.Functions.ILike(a.ActorEmail, $"%{actorEmail}%"));
        }
        if (!string.IsNullOrEmpty(actionName))
        {
            query = query.Where(a => EF.Functions.ILike(a.ActionName, $"%{actionName}%"));
        }
        if (!string.IsNullOrEmpty(entityName))
        {
            query = query.Where(a => EF.Functions.ILike(a.EntityName, $"%{entityName}%"));
        }

        var totalItems = await query.CountAsync();
        var logs = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse
            {
                Id = a.Id,
                ActionName = a.ActionName,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                ActorEmail = a.ActorEmail,
                DetailsJson = a.DetailsJson,
                CreatedAtUtc = a.CreatedAtUtc
            })
            .ToListAsync();

        Response.Headers["X-Total-Count"] = totalItems.ToString();
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();

        return Ok(logs);
    }
}
