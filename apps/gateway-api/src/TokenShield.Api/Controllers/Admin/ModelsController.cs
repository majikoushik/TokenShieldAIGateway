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

public class ModelsController : AdminControllerBase
{
    private readonly TokenShieldDbContext _context;
    private readonly IAuditLoggingService _auditLoggingService;

    public ModelsController(TokenShieldDbContext context, IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _auditLoggingService = auditLoggingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ModelResponse>>> GetModels()
    {
        var tenantId = await GetTenantIdAsync(_context);

        var models = await _context.AiModels
            .Include(m => m.Provider)
            .Where(m => m.Provider.TenantId == tenantId)
            .OrderBy(m => m.Provider.Name).ThenBy(m => m.Name)
            .Select(m => new ModelResponse
            {
                Id = m.Id,
                ProviderId = m.ProviderId,
                ProviderName = m.Provider.Name,
                Name = m.Name,
                DeploymentName = m.DeploymentName,
                Tier = m.Tier,
                InputTokenPricePerMillion = m.InputTokenPricePerMillion,
                OutputTokenPricePerMillion = m.OutputTokenPricePerMillion,
                ContextWindow = m.ContextWindow,
                IsActive = m.IsActive,
                CreatedAtUtc = m.CreatedAtUtc,
                UpdatedAtUtc = m.UpdatedAtUtc
            })
            .ToListAsync();

        return Ok(models);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ModelResponse>> GetModel(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var model = await _context.AiModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id && m.Provider.TenantId == tenantId);

        if (model == null)
        {
            return NotFound(new { error = "Model not found." });
        }

        var response = new ModelResponse
        {
            Id = model.Id,
            ProviderId = model.ProviderId,
            ProviderName = model.Provider.Name,
            Name = model.Name,
            DeploymentName = model.DeploymentName,
            Tier = model.Tier,
            InputTokenPricePerMillion = model.InputTokenPricePerMillion,
            OutputTokenPricePerMillion = model.OutputTokenPricePerMillion,
            ContextWindow = model.ContextWindow,
            IsActive = model.IsActive,
            CreatedAtUtc = model.CreatedAtUtc,
            UpdatedAtUtc = model.UpdatedAtUtc
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ModelResponse>> CreateModel([FromBody] CreateModelRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new CreateModelRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        // Validate provider existence and tenant ownership
        var provider = await _context.ModelProviders
            .FirstOrDefaultAsync(p => p.Id == request.ProviderId && p.TenantId == tenantId);

        if (provider == null)
        {
            return BadRequest(new { error = "Invalid ProviderId specified or provider does not belong to your tenant." });
        }

        var model = new AiModel
        {
            ProviderId = request.ProviderId,
            Name = request.Name,
            DeploymentName = request.DeploymentName,
            Tier = request.Tier,
            InputTokenPricePerMillion = request.InputTokenPricePerMillion,
            OutputTokenPricePerMillion = request.OutputTokenPricePerMillion,
            ContextWindow = request.ContextWindow,
            IsActive = request.IsActive
        };

        _context.AiModels.Add(model);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "CreateModel",
            "AiModel",
            model.Id,
            GetActorEmail(),
            new
            {
                providerId = model.ProviderId,
                name = model.Name,
                deploymentName = model.DeploymentName,
                tier = model.Tier,
                inputPrice = model.InputTokenPricePerMillion,
                outputPrice = model.OutputTokenPricePerMillion,
                contextWindow = model.ContextWindow,
                isActive = model.IsActive
            });

        var response = new ModelResponse
        {
            Id = model.Id,
            ProviderId = model.ProviderId,
            ProviderName = provider.Name,
            Name = model.Name,
            DeploymentName = model.DeploymentName,
            Tier = model.Tier,
            InputTokenPricePerMillion = model.InputTokenPricePerMillion,
            OutputTokenPricePerMillion = model.OutputTokenPricePerMillion,
            ContextWindow = model.ContextWindow,
            IsActive = model.IsActive,
            CreatedAtUtc = model.CreatedAtUtc,
            UpdatedAtUtc = model.UpdatedAtUtc
        };

        return CreatedAtAction(nameof(GetModel), new { id = model.Id }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateModel(Guid id, [FromBody] UpdateModelRequest request)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var validator = new UpdateModelRequestValidator();
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }

        var model = await _context.AiModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id && m.Provider.TenantId == tenantId);

        if (model == null)
        {
            return NotFound(new { error = "Model not found." });
        }

        model.Name = request.Name;
        model.DeploymentName = request.DeploymentName;
        model.Tier = request.Tier;
        model.InputTokenPricePerMillion = request.InputTokenPricePerMillion;
        model.OutputTokenPricePerMillion = request.OutputTokenPricePerMillion;
        model.ContextWindow = request.ContextWindow;
        model.IsActive = request.IsActive;
        model.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "UpdateModel",
            "AiModel",
            model.Id,
            GetActorEmail(),
            new
            {
                name = model.Name,
                deploymentName = model.DeploymentName,
                tier = model.Tier,
                inputPrice = model.InputTokenPricePerMillion,
                outputPrice = model.OutputTokenPricePerMillion,
                contextWindow = model.ContextWindow,
                isActive = model.IsActive
            });

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteModel(Guid id)
    {
        var tenantId = await GetTenantIdAsync(_context);

        var model = await _context.AiModels
            .Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id && m.Provider.TenantId == tenantId);

        if (model == null)
        {
            return NotFound(new { error = "Model not found." });
        }

        model.IsDeleted = true;
        model.IsActive = false;
        model.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            tenantId,
            "DeleteModel",
            "AiModel",
            model.Id,
            GetActorEmail(),
            new { name = model.Name });

        return NoContent();
    }
}
