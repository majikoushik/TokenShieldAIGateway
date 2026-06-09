using TokenShield.Domain.Common;

namespace TokenShield.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    
    // Navigation properties
    public ICollection<ClientApplication> ClientApplications { get; set; } = new List<ClientApplication>();
    public ICollection<ModelProvider> ModelProviders { get; set; } = new List<ModelProvider>();
    public ICollection<RoutingRule> RoutingRules { get; set; } = new List<RoutingRule>();
    public ICollection<BudgetLimit> BudgetLimits { get; set; } = new List<BudgetLimit>();
}
