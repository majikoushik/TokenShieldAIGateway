using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TokenShield.Domain.Common;
using TokenShield.Domain.Entities;

namespace TokenShield.Infrastructure.Persistence;

public class TokenShieldDbContext : DbContext
{
    public TokenShieldDbContext(DbContextOptions<TokenShieldDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<ClientApplication> ClientApplications => Set<ClientApplication>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ModelProvider> ModelProviders => Set<ModelProvider>();
    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<BudgetLimit> BudgetLimits => Set<BudgetLimit>();
    public DbSet<AiRequestLog> AiRequestLogs => Set<AiRequestLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Soft Delete global filters
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
            }
        }

        // Configure Tenant indexes and properties
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
        });

        // Configure ClientApplication
        modelBuilder.Entity<ClientApplication>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(c => c.TenantId);
            entity.HasIndex(c => new { c.TenantId, c.IsDeleted });
        });

        // Configure ApiKey
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.Property(k => k.Name).IsRequired().HasMaxLength(200);
            entity.Property(k => k.KeyHash).IsRequired().HasMaxLength(256);
            entity.Property(k => k.Prefix).IsRequired().HasMaxLength(20);
            entity.HasIndex(k => k.KeyHash).IsUnique();
            entity.HasIndex(k => k.TenantId);
            entity.HasIndex(k => new { k.TenantId, k.IsDeleted });
        });

        // Configure ModelProvider
        modelBuilder.Entity<ModelProvider>(entity =>
        {
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.ApiUrl).IsRequired().HasMaxLength(500);
            entity.Property(p => p.ApiKeySecretRef).IsRequired().HasMaxLength(500);
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => new { p.TenantId, p.IsDeleted });
        });

        // Configure AiModel
        modelBuilder.Entity<AiModel>(entity =>
        {
            entity.Property(m => m.Name).IsRequired().HasMaxLength(100);
            entity.Property(m => m.DeploymentName).IsRequired().HasMaxLength(200);
            entity.Property(m => m.InputTokenPricePerMillion).HasPrecision(18, 4);
            entity.Property(m => m.OutputTokenPricePerMillion).HasPrecision(18, 4);
            entity.Property(m => m.Tier).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(m => m.ProviderId);
        });

        // Configure RoutingRule
        modelBuilder.Entity<RoutingRule>(entity =>
        {
            entity.Property(r => r.Name).IsRequired().HasMaxLength(200);
            entity.Property(r => r.ConditionsJson).HasColumnType("jsonb");
            entity.Property(r => r.Action).HasConversion<string>().HasMaxLength(50);
            entity.Property(r => r.TargetTier).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(r => r.TenantId);
            entity.HasIndex(r => new { r.TenantId, r.IsDeleted });
        });

        // Configure BudgetLimit
        modelBuilder.Entity<BudgetLimit>(entity =>
        {
            entity.Property(b => b.MonthlyLimit).HasPrecision(18, 4);
            entity.Property(b => b.WarningThresholdPercent).HasPrecision(5, 2);
            entity.Property(b => b.CurrentSpend).HasPrecision(18, 4);
            entity.Property(b => b.Scope).HasConversion<string>().HasMaxLength(50);
            entity.Property(b => b.Action).HasConversion<string>().HasMaxLength(50);
            entity.HasIndex(b => b.TenantId);
            entity.HasIndex(b => new { b.TenantId, b.Scope, b.TargetId });
        });

        // Configure AiRequestLog
        modelBuilder.Entity<AiRequestLog>(entity =>
        {
            entity.Property(l => l.RequestId).IsRequired().HasMaxLength(100);
            entity.Property(l => l.PromptHash).IsRequired().HasMaxLength(64);
            entity.Property(l => l.ResponseHash).IsRequired().HasMaxLength(64);
            entity.Property(l => l.EstimatedCost).HasPrecision(18, 6);
            entity.Property(l => l.SelectedProvider).IsRequired().HasMaxLength(100);
            entity.Property(l => l.SelectedModel).IsRequired().HasMaxLength(100);
            entity.Property(l => l.SelectedTier).IsRequired().HasMaxLength(50);
            entity.Property(l => l.BudgetStatus).IsRequired().HasMaxLength(50);
            entity.Property(l => l.RequestStatus).IsRequired().HasMaxLength(50);
            entity.Property(l => l.MatchedRuleName).HasMaxLength(200);
            entity.HasIndex(l => l.TenantId);
            entity.HasIndex(l => l.CorrelationId);
            entity.HasIndex(l => l.CreatedAtUtc);
        });

        // Configure AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(a => a.ActionName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.ActorEmail).IsRequired().HasMaxLength(256);
            entity.Property(a => a.DetailsJson).HasColumnType("jsonb");
            entity.HasIndex(a => a.TenantId);
            entity.HasIndex(a => a.CreatedAtUtc);
        });
    }

    private static LambdaExpression GetSoftDeleteFilter(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
        var body = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(body, parameter);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IHasTimestamps && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IHasTimestamps)entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAtUtc = now;
            }

            entity.UpdatedAtUtc = now;
        }
    }
}
