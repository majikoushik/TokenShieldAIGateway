namespace TokenShield.Domain.Enums;

public enum ModelTier
{
    Cheap,
    Standard,
    Premium
}

public enum RoutingActionType
{
    RouteToTier,
    HumanReview,
    Block
}

public enum BudgetScope
{
    Tenant,
    Application,
    ApiKey,
    Model
}

public enum BudgetActionType
{
    WarnOnly,
    Block,
    Downgrade
}
