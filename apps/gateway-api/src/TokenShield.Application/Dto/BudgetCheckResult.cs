namespace TokenShield.Application.Dto;

public class BudgetCheckResult
{
    public bool IsBlocked { get; set; }
    public bool IsWarning { get; set; }
    public bool IsDowngraded { get; set; }
    public string BudgetStatus { get; set; } = "Within Limits";
    public string? WarningMessage { get; set; }
}
