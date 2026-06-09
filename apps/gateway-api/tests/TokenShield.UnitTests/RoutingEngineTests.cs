using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TokenShield.Application.Common.Interfaces;
using TokenShield.Application.Dto;
using TokenShield.CostEngine.Services;
using TokenShield.Domain.Entities;
using TokenShield.Domain.Enums;
using TokenShield.Domain.Models;
using TokenShield.Guardrails.Profiling;
using TokenShield.Infrastructure.Persistence;
using TokenShield.PolicyEngine.Engine;
using Xunit;

namespace TokenShield.UnitTests;

public class RoutingEngineTests
{
    private readonly CostEngineService _costEngine = new();
    private readonly MvpRequestProfiler _profiler = new();

    [Theory]
    [InlineData("hello", 2)] // 5 chars -> 2 tokens
    [InlineData("", 0)]
    [InlineData("this is a longer sentence.", 7)] // 26 chars -> 7 tokens
    public void CostEngine_EstimateTokens_ApproximatesCorrectly(string text, int expectedTokens)
    {
        var result = _costEngine.EstimateTokens(text);
        Assert.Equal(expectedTokens, result);
    }

    [Fact]
    public void CostEngine_CalculateCost_UsesDecimalArithmetic()
    {
        // Arrange
        int tokens = 1500;
        decimal pricePerMillion = 2.50m; // E.g. $2.50 per M

        // Act
        var cost = _costEngine.CalculateCost(tokens, pricePerMillion);

        // Assert
        Assert.Equal(0.003750m, cost); // (1500 / 1000000) * 2.5
    }

    [Theory]
    [InlineData("Please summarize this case document.", "summarization")]
    [InlineData("Translate this content to Spanish.", "translation")]
    [InlineData("Write a Python script to sort a list.", "coding")]
    [InlineData("Analyze this data and think logically.", "complex_reasoning")]
    [InlineData("Hello there, how are you?", "general")]
    public async Task RequestProfiler_InfersTaskType_FromKeywords(string prompt, string expectedTaskType)
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new ChatMessage { Role = "user", Content = prompt } }
        };

        // Act
        var profile = await _profiler.ProfileRequestAsync(request, 10);

        // Assert
        Assert.Equal(expectedTaskType, profile.TaskType);
    }

    [Theory]
    [InlineData("My email address is support@acme.com.", true)]
    [InlineData("Call me at 123-456-7890 if urgent.", true)]
    [InlineData("No sensitive data in this sentence.", false)]
    public async Task RequestProfiler_ScansForPii_Correctly(string prompt, bool expectedContainsPii)
    {
        // Arrange
        var request = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new ChatMessage { Role = "user", Content = prompt } }
        };

        // Act
        var profile = await _profiler.ProfileRequestAsync(request, 10);

        // Assert
        Assert.Equal(expectedContainsPii, profile.ContainsPii);
    }

    [Fact]
    public async Task RequestProfiler_ComplexityScore_FollowsMvpRules()
    {
        // Arrange - Base request
        var request1 = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new ChatMessage { Role = "user", Content = "Short prompt" } }
        };
        
        // Act & Assert 1 (Base complexity 20)
        var profile1 = await _profiler.ProfileRequestAsync(request1, 100);
        Assert.Equal(20, profile1.ComplexityScore);

        // Arrange - High tokens (> 4000)
        var profile2 = await _profiler.ProfileRequestAsync(request1, 4500);
        Assert.Equal(40, profile2.ComplexityScore); // 20 base + 20 tokens

        // Arrange - Reasoning required metadata + complex_reasoning taskType
        var request3 = new ChatCompletionRequest
        {
            Model = "auto",
            Messages = new() { new ChatMessage { Role = "user", Content = "Analyze and reason through this logic problem." } },
            Metadata = new() { { "requiresReasoning", "true" } }
        };
        var profile3 = await _profiler.ProfileRequestAsync(request3, 100);
        Assert.Equal(70, profile3.ComplexityScore); // 20 base + 30 reasoning + 20 inferred complex_reasoning taskType
    }

    [Fact]
    public async Task RoutingRuleEngine_DefaultRouting_AppliesStandardTier()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();

        var engine = new RoutingRuleEngine(context);
        var profile = new RequestProfile
        {
            TaskType = "general",
            RiskLevel = "medium",
            InputTokens = 100,
            ComplexityScore = 20,
            ContainsPii = false
        };

        // Act
        var (action, selectedTier, matchedRuleName) = await engine.MatchRuleAsync(Guid.NewGuid(), profile);

        // Assert
        Assert.Equal(RoutingActionType.RouteToTier, action);
        Assert.Equal(ModelTier.Standard, selectedTier);
        Assert.Equal("Default Routing", matchedRuleName);
    }

    [Fact]
    public async Task RoutingRuleEngine_HighRiskRequest_RequiresHumanReviewByDefault()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();

        var engine = new RoutingRuleEngine(context);
        var profile = new RequestProfile
        {
            TaskType = "general",
            RiskLevel = "high", // High risk
            InputTokens = 100,
            ComplexityScore = 20,
            ContainsPii = false
        };

        // Act
        var (action, selectedTier, matchedRuleName) = await engine.MatchRuleAsync(Guid.NewGuid(), profile);

        // Assert
        Assert.Equal(RoutingActionType.HumanReview, action);
        Assert.Null(selectedTier);
        Assert.Equal("Default High Risk Policy", matchedRuleName);
    }

    [Fact]
    public async Task RoutingRuleEngine_EvaluatesRulesInPriorityOrder()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<TokenShieldDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TokenShieldDbContext>();

        var tenantId = Guid.NewGuid();

        // Seed some custom conflicting rules:
        // Priority 1: Block PII
        // Priority 2: Route Summarization to Cheap
        context.RoutingRules.Add(new RoutingRule
        {
            TenantId = tenantId,
            Name = "Block PII Rule",
            Priority = 1,
            ConditionsJson = "[{\"field\":\"containsPii\",\"operator\":\"Equals\",\"value\":\"true\"}]",
            Action = RoutingActionType.Block,
            TargetTier = null,
            IsActive = true
        });

        context.RoutingRules.Add(new RoutingRule
        {
            TenantId = tenantId,
            Name = "Summarization Rule",
            Priority = 2,
            ConditionsJson = "[{\"field\":\"taskType\",\"operator\":\"Equals\",\"value\":\"summarization\"}]",
            Action = RoutingActionType.RouteToTier,
            TargetTier = ModelTier.Cheap,
            IsActive = true
        });

        await context.SaveChangesAsync();

        var engine = new RoutingRuleEngine(context);

        // Act 1 - Request containing PII and Summarization (Priority 1: Block PII should win)
        var profile1 = new RequestProfile
        {
            TaskType = "summarization",
            RiskLevel = "low",
            ContainsPii = true,
            InputTokens = 100,
            ComplexityScore = 20
        };
        var (action1, tier1, rule1) = await engine.MatchRuleAsync(tenantId, profile1);

        // Assert 1
        Assert.Equal(RoutingActionType.Block, action1);
        Assert.Null(tier1);
        Assert.Equal("Block PII Rule", rule1);

        // Act 2 - Request containing Summarization but no PII (Priority 2: Summarization should win)
        var profile2 = new RequestProfile
        {
            TaskType = "summarization",
            RiskLevel = "low",
            ContainsPii = false,
            InputTokens = 100,
            ComplexityScore = 20
        };
        var (action2, tier2, rule2) = await engine.MatchRuleAsync(tenantId, profile2);

        // Assert 2
        Assert.Equal(RoutingActionType.RouteToTier, action2);
        Assert.Equal(ModelTier.Cheap, tier2);
        Assert.Equal("Summarization Rule", rule2);
    }
}
