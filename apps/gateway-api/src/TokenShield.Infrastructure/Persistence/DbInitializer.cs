using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;

namespace TokenShield.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, bool seedData = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TokenShieldDbContext>>();

        try
        {
            // Apply migrations automatically if not in testing (in-memory db context throws if Database.Migrate is called)
            if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                logger.LogInformation("Applying pending database migrations...");
                await context.Database.MigrateAsync();
            }

            if (seedData)
            {
                logger.LogInformation("Running database seeding checks...");
                await SeedDataAsync(context, logger);
            }
            else
            {
                logger.LogInformation("Database seeding skipped (SeedDatabase=false).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during database migration or seeding.");
            throw;
        }
    }

    private static async Task SeedDataAsync(TokenShieldDbContext context, ILogger logger)
    {
        // 1. Seed Tenant
        var tenantName = "Acme Enterprise";
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Name == tenantName);
        if (tenant == null)
        {
            tenant = new Tenant { Name = tenantName };
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded Tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
        }

        // 2. Seed Client Application (Team app representing developer team)
        var appName = "Acme Developer Portal";
        var clientApp = await context.ClientApplications.FirstOrDefaultAsync(a => a.TenantId == tenant.Id && a.Name == appName);
        if (clientApp == null)
        {
            clientApp = new ClientApplication
            {
                TenantId = tenant.Id,
                Name = appName
            };
            context.ClientApplications.Add(clientApp);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded Client Application: {AppName}", clientApp.Name);
        }

        // 3. Seed API Key (Hashed ts_dev_acmedeveloperkey12345)
        var keyName = "Acme Primary Dev Key";
        var rawKey = "ts_dev_acmedeveloperkey12345";
        var keyHash = HashKey(rawKey);
        
        var apiKey = await context.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash);
        if (apiKey == null)
        {
            apiKey = new ApiKey
            {
                TenantId = tenant.Id,
                ClientApplicationId = clientApp.Id,
                Name = keyName,
                Prefix = "ts_dev_",
                KeyHash = keyHash,
                ExpiresAtUtc = DateTime.UtcNow.AddYears(1),
                IsRevoked = false
            };
            context.ApiKeys.Add(apiKey);
            await context.SaveChangesAsync();
            var maskedKey = $"{apiKey.Prefix}***{rawKey.Substring(rawKey.Length - 4)}";
            logger.LogInformation("Seeded API Key: {KeyName} (Masked: '{MaskedKey}')", apiKey.Name, maskedKey);
        }

        // 4. Seed Providers
        var providers = new List<(string Name, string Url)>
        {
            ("Mock Provider", "http://localhost:5000/v1/mock"),
            ("OpenAI", "https://api.openai.com/v1"),
            ("Azure OpenAI", "https://acme-openai.openai.azure.com"),
            ("Anthropic", "https://api.anthropic.com/v1")
        };

        var providerEntities = new Dictionary<string, ModelProvider>();

        foreach (var p in providers)
        {
            var provider = await context.ModelProviders.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Name == p.Name);
            if (provider == null)
            {
                provider = new ModelProvider
                {
                    TenantId = tenant.Id,
                    Name = p.Name,
                    ApiUrl = p.Url,
                    ApiKeySecretRef = $"kv-secret-provider-{p.Name.Replace(" ", "-").ToLower()}",
                    IsActive = true
                };
                context.ModelProviders.Add(provider);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded Model Provider: {ProviderName}", provider.Name);
            }
            providerEntities[p.Name] = provider;
        }

        // 5. Seed Models
        var models = new List<(string ProviderName, string Name, string DeployName, ModelTier Tier, decimal InputPrice, decimal OutputPrice, int Context)>
        {
            // Mock Models
            ("Mock Provider", "mock-cheap", "mock-cheap-deployment", ModelTier.Cheap, 0.10m, 0.20m, 8192),
            ("Mock Provider", "mock-standard", "mock-standard-deployment", ModelTier.Standard, 1.00m, 2.00m, 16384),
            ("Mock Provider", "mock-premium", "mock-premium-deployment", ModelTier.Premium, 10.00m, 20.00m, 32768),
            // OpenAI Models
            ("OpenAI", "gpt-4o-mini", "gpt-4o-mini", ModelTier.Cheap, 0.150m, 0.600m, 128000),
            ("OpenAI", "gpt-4o", "gpt-4o", ModelTier.Standard, 2.500m, 10.000m, 128000),
            ("OpenAI", "o1-preview", "o1-preview", ModelTier.Premium, 15.000m, 60.000m, 128000),
            // Azure OpenAI Models
            ("Azure OpenAI", "gpt-4o-mini", "deploy-gpt-4o-mini", ModelTier.Cheap, 0.150m, 0.600m, 128000),
            ("Azure OpenAI", "gpt-4o", "deploy-gpt-4o", ModelTier.Standard, 2.500m, 10.000m, 128000),
            // Anthropic Models
            ("Anthropic", "claude-3-5-haiku", "claude-3-5-haiku-20241022", ModelTier.Cheap, 0.800m, 4.000m, 200000),
            ("Anthropic", "claude-3-5-sonnet", "claude-3-5-sonnet-20241022", ModelTier.Standard, 3.000m, 15.000m, 200000),
            ("Anthropic", "claude-3-opus", "claude-3-opus-20240229", ModelTier.Premium, 15.000m, 75.000m, 200000)
        };

        foreach (var m in models)
        {
            if (providerEntities.TryGetValue(m.ProviderName, out var provider))
            {
                var model = await context.AiModels.FirstOrDefaultAsync(x => x.ProviderId == provider.Id && x.Name == m.Name);
                if (model == null)
                {
                    model = new AiModel
                    {
                        ProviderId = provider.Id,
                        Name = m.Name,
                        DeploymentName = m.DeployName,
                        Tier = m.Tier,
                        InputTokenPricePerMillion = m.InputPrice,
                        OutputTokenPricePerMillion = m.OutputPrice,
                        ContextWindow = m.Context,
                        IsActive = true
                    };
                    context.AiModels.Add(model);
                    logger.LogInformation("Seeded AI Model: {ModelName} under Provider {ProviderName}", model.Name, m.ProviderName);
                }
            }
        }
        await context.SaveChangesAsync();

        // 6. Seed Routing Rules
        var rules = new List<(string Name, int Priority, string Conditions, RoutingActionType Action, ModelTier? Target)>
        {
            ("Low-risk summarization to Cheap", 1, "[{\"field\":\"riskLevel\",\"operator\":\"Equals\",\"value\":\"low\"},{\"field\":\"taskType\",\"operator\":\"Equals\",\"value\":\"summarization\"}]", RoutingActionType.RouteToTier, ModelTier.Cheap),
            ("High complexity requests to Premium", 2, "[{\"field\":\"complexityScore\",\"operator\":\"GreaterThan\",\"value\":\"80\"}]", RoutingActionType.RouteToTier, ModelTier.Premium),
            ("Block suspicious PII request logs", 3, "[{\"field\":\"containsPii\",\"operator\":\"Equals\",\"value\":\"true\"}]", RoutingActionType.Block, null)
        };

        foreach (var r in rules)
        {
            var rule = await context.RoutingRules.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Name == r.Name);
            if (rule == null)
            {
                rule = new RoutingRule
                {
                    TenantId = tenant.Id,
                    Name = r.Name,
                    Priority = r.Priority,
                    ConditionsJson = r.Conditions,
                    Action = r.Action,
                    TargetTier = r.Target,
                    IsActive = true
                };
                context.RoutingRules.Add(rule);
                logger.LogInformation("Seeded Routing Rule: {RuleName}", rule.Name);
            }
        }
        await context.SaveChangesAsync();

        // 6.5 Seed Profiler Rules
        var profilerRules = new List<(string Name, string TaskType, string Phrases, string RegexPatterns, double Confidence, int Priority)>
        {
            ("Summarization Task", "summarization", "[\"summarize\",\"tl;dr\",\"key points\"]", "[]", 0.82, 10),
            ("Code Review Task", "code_review", "[\"review this code\",\"find bugs in this PR\"]", "[]", 0.85, 20),
            ("Translation Task", "translation", "[\"translate to\",\"translate this\"]", "[]", 0.90, 30)
        };

        foreach (var pr in profilerRules)
        {
            var pRule = await context.ProfilerRules.FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.Name == pr.Name);
            if (pRule == null)
            {
                pRule = new ProfilerRule
                {
                    TenantId = tenant.Id,
                    Name = pr.Name,
                    TargetTaskType = pr.TaskType,
                    PhrasesJson = pr.Phrases,
                    RegexPatternsJson = pr.RegexPatterns,
                    Confidence = pr.Confidence,
                    Priority = pr.Priority,
                    IsActive = true
                };
                context.ProfilerRules.Add(pRule);
                logger.LogInformation("Seeded Profiler Rule: {RuleName}", pRule.Name);
            }
        }
        await context.SaveChangesAsync();

        // 7. Seed Budgets
        // Tenant Budget
        var tenantBudget = await context.BudgetLimits.FirstOrDefaultAsync(b => b.TenantId == tenant.Id && b.Scope == BudgetScope.Tenant);
        if (tenantBudget == null)
        {
            tenantBudget = new BudgetLimit
            {
                TenantId = tenant.Id,
                Scope = BudgetScope.Tenant,
                TargetId = null,
                MonthlyLimit = 5000.00m,
                WarningThresholdPercent = 80.00m,
                CurrentSpend = 1245.89m, // Mock some active usage
                LastResetAtUtc = DateTime.UtcNow.AddDays(-DateTime.UtcNow.Day + 1), // Beginning of month
                Action = BudgetActionType.WarnOnly
            };
            context.BudgetLimits.Add(tenantBudget);
            logger.LogInformation("Seeded Tenant Budget Limit: ${Limit}", tenantBudget.MonthlyLimit);
        }

        // Application Budget
        var appBudget = await context.BudgetLimits.FirstOrDefaultAsync(b => b.TenantId == tenant.Id && b.Scope == BudgetScope.Application && b.TargetId == clientApp.Id);
        if (appBudget == null)
        {
            appBudget = new BudgetLimit
            {
                TenantId = tenant.Id,
                Scope = BudgetScope.Application,
                TargetId = clientApp.Id,
                MonthlyLimit = 1000.00m,
                WarningThresholdPercent = 90.00m,
                CurrentSpend = 420.50m,
                LastResetAtUtc = DateTime.UtcNow.AddDays(-DateTime.UtcNow.Day + 1),
                Action = BudgetActionType.Block
            };
            context.BudgetLimits.Add(appBudget);
            logger.LogInformation("Seeded Application Budget Limit: ${Limit} for App {AppName}", appBudget.MonthlyLimit, clientApp.Name);
        }
        await context.SaveChangesAsync();

        // 8. Seed initial Audit Log describing database initialization
        var dbInitAudit = await context.AuditLogs.FirstOrDefaultAsync(a => a.TenantId == tenant.Id && a.ActionName == "DatabaseSeeded");
        if (dbInitAudit == null)
        {
            context.AuditLogs.Add(new AuditLog
            {
                TenantId = tenant.Id,
                ActionName = "DatabaseSeeded",
                EntityName = "Database",
                EntityId = Guid.Empty,
                ActorEmail = "admin@acme.com",
                DetailsJson = "{\"message\":\"Idempotent development seed database initialized successfully.\"}"
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded initial system Audit Log entry");
        }
    }

    private static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
