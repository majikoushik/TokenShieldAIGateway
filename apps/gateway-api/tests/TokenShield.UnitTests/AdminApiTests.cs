using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TokenShield.Api.Controllers.Admin;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto.Admin;
using TokenShield.Application.Services;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Infrastructure.Persistence;
using TokenShield.Observability.Services;
using Xunit;

namespace TokenShield.UnitTests;

public class AdminApiTests
{
    private (TokenShieldDbContext DbContext, IAuditLoggingService AuditService) CreateTestContext()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();
        var serviceProvider = services.BuildServiceProvider();

        var context = serviceProvider.GetRequiredService<TokenShieldDbContext>();
        var audit = serviceProvider.GetRequiredService<IAuditLoggingService>();
        return (context, audit);
    }

    private void SetupControllerContext(ControllerBase controller, Guid tenantId, string email = "admin@test.com")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-tenant-id"] = tenantId.ToString();
        httpContext.Request.Headers["x-user-email"] = email;
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task ProvidersController_GetAndCreate_EnforcesTenantIsolationAndAudit()
    {
        // Arrange
        var (context, audit) = CreateTestContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed Tenant A and B in database
        context.Tenants.AddRange(
            new Tenant { Id = tenantA, Name = "Tenant A" },
            new Tenant { Id = tenantB, Name = "Tenant B" }
        );
        await context.SaveChangesAsync();

        var controller = new ProvidersController(context, audit);
        SetupControllerContext(controller, tenantA);

        // Act & Assert 1: Create a provider for Tenant A
        var createRequest = new CreateProviderRequest
        {
            Name = "OpenAI Tenant A",
            ApiUrl = "https://api.openai.com/v1",
            ApiKeySecretRef = "kv-secret-openai-a"
        };
        var createResult = await controller.CreateProvider(createRequest);
        var createdResponse = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var provider = Assert.IsType<ProviderResponse>(createdResponse.Value);
        Assert.Equal("OpenAI Tenant A", provider.Name);

        // Verify Audit Log was written for Tenant A
        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(l => l.TenantId == tenantA && l.ActionName == "CreateProvider");
        Assert.NotNull(auditLog);
        Assert.Contains("kv-secret-openai-a", auditLog.DetailsJson);

        // Act & Assert 2: List providers as Tenant A (returns 1 item)
        var listResultA = await controller.GetProviders();
        var listA = Assert.IsType<OkObjectResult>(listResultA.Result).Value as List<ProviderResponse>;
        Assert.NotNull(listA);
        Assert.Single(listA);

        // Act & Assert 3: List providers as Tenant B (returns 0 items)
        var controllerB = new ProvidersController(context, audit);
        SetupControllerContext(controllerB, tenantB);
        var listResultB = await controllerB.GetProviders();
        var listB = Assert.IsType<OkObjectResult>(listResultB.Result).Value as List<ProviderResponse>;
        Assert.NotNull(listB);
        Assert.Empty(listB);
    }

    [Fact]
    public async Task ModelsController_ValidationAndIsolation_FailsOnNegativePrice()
    {
        // Arrange
        var (context, audit) = CreateTestContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "Acme" });
        
        var provider = new ModelProvider
        {
            TenantId = tenantId,
            Name = "OpenAI",
            ApiUrl = "https://api.openai.com/v1",
            ApiKeySecretRef = "secret"
        };
        context.ModelProviders.Add(provider);
        await context.SaveChangesAsync();

        var controller = new ModelsController(context, audit);
        SetupControllerContext(controller, tenantId);

        // Act & Assert: Create with negative input token price
        var badRequest = new CreateModelRequest
        {
            ProviderId = provider.Id,
            Name = "gpt-4",
            DeploymentName = "gpt-4",
            Tier = ModelTier.Standard,
            InputTokenPricePerMillion = -0.5m,
            OutputTokenPricePerMillion = 2.0m,
            ContextWindow = 8192
        };

        var result = await controller.CreateModel(badRequest);
        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badResult.Value);
    }

    [Fact]
    public async Task ApiKeysController_CreateAndRead_ExcludesSensitiveKeyDetails()
    {
        // Arrange
        var (context, audit) = CreateTestContext();
        var tenantId = Guid.NewGuid();
        context.Tenants.Add(new Tenant { Id = tenantId, Name = "Acme" });

        var app = new ClientApplication { TenantId = tenantId, Name = "App A" };
        context.ClientApplications.Add(app);
        await context.SaveChangesAsync();

        var mockApiKeyService = new ApiKeyService();
        var controller = new ApiKeysController(context, mockApiKeyService, audit);
        SetupControllerContext(controller, tenantId);

        // Act 1: Create Key
        var createResult = await controller.CreateApiKey(new CreateApiKeyRequest
        {
            ClientApplicationId = app.Id,
            Name = "Production Key",
            Prefix = "ts_live_"
        });

        var createdResponse = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdKey = Assert.IsType<ApiKeyCreatedResponse>(createdResponse.Value);
        Assert.StartsWith("ts_live_", createdKey.RawKey); // Raw key returned ONCE here

        // Act 2: Read details
        var getResult = await controller.GetApiKey(createdKey.Id);
        var keyDetail = Assert.IsType<OkObjectResult>(getResult.Result).Value as ApiKeyResponse;
        
        Assert.NotNull(keyDetail);
        Assert.Equal("Production Key", keyDetail.Name);
        
        // Assert: Response DTO lacks any KeyHash or raw key fields
        var properties = typeof(ApiKeyResponse).GetProperties();
        Assert.Empty(properties.Where(p => p.Name.Equals("KeyHash", StringComparison.OrdinalIgnoreCase) || p.Name.Equals("RawKey", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task UsageAnalyticsController_Summary_CalculatesAggregatedSpendAndLatency()
    {
        // Arrange
        var (context, _) = CreateTestContext();
        var tenantId = Guid.NewGuid();

        // Seed logs
        context.AiRequestLogs.AddRange(
            new AiRequestLog
            {
                RequestId = "req-1",
                TenantId = tenantId,
                ApplicationId = Guid.NewGuid(),
                ApiKeyId = Guid.NewGuid(),
                PromptHash = "hash1",
                ResponseHash = "hash2",
                InputTokens = 1000,
                OutputTokens = 500,
                EstimatedCost = 0.0035m,
                SelectedProvider = "OpenAI",
                SelectedModel = "gpt-4o",
                SelectedTier = "standard",
                RequestStatus = "Success",
                BudgetStatus = "Within Limits",
                LatencyMs = 150
            },
            new AiRequestLog
            {
                RequestId = "req-2",
                TenantId = tenantId,
                ApplicationId = Guid.NewGuid(),
                ApiKeyId = Guid.NewGuid(),
                PromptHash = "hash3",
                ResponseHash = "hash4",
                InputTokens = 2000,
                OutputTokens = 1000,
                EstimatedCost = 0.0070m,
                SelectedProvider = "OpenAI",
                SelectedModel = "gpt-4o",
                SelectedTier = "standard",
                RequestStatus = "Success",
                BudgetStatus = "Within Limits",
                LatencyMs = 250
            }
        );
        await context.SaveChangesAsync();

        var controller = new UsageAnalyticsController(context);
        SetupControllerContext(controller, tenantId);

        // Act
        var result = await controller.GetSummary();
        var summary = Assert.IsType<OkObjectResult>(result.Result).Value as DashboardSummaryResponse;

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(0.0105m, summary.TotalCost);
        Assert.Equal(2, summary.TotalRequests);
        Assert.Equal(3000, summary.TotalInputTokens);
        Assert.Equal(200, summary.AverageLatencyMs); // (150 + 250) / 2 = 200
        Assert.Single(summary.CostByModel);
        Assert.Equal("gpt-4o", summary.CostByModel[0].GroupKey);
    }
}
