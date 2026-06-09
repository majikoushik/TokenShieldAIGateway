using FluentValidation;
using TokenShield.Application.Dto.Admin;

namespace TokenShield.Application.Validators.Admin;

// --- Provider Validators ---
public class CreateProviderRequestValidator : AbstractValidator<CreateProviderRequest>
{
    public CreateProviderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Provider Name is required.");
        RuleFor(x => x.ApiUrl).NotEmpty().WithMessage("ApiUrl is required.")
            .Must(url => url.StartsWith("http://") || url.StartsWith("https://"))
            .WithMessage("ApiUrl must start with http:// or https://");
        RuleFor(x => x.ApiKeySecretRef).NotEmpty().WithMessage("ApiKeySecretRef is required.");
    }
}

public class UpdateProviderRequestValidator : AbstractValidator<UpdateProviderRequest>
{
    public UpdateProviderRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Provider Name is required.");
        RuleFor(x => x.ApiUrl).NotEmpty().WithMessage("ApiUrl is required.")
            .Must(url => url.StartsWith("http://") || url.StartsWith("https://"))
            .WithMessage("ApiUrl must start with http:// or https://");
        RuleFor(x => x.ApiKeySecretRef).NotEmpty().WithMessage("ApiKeySecretRef is required.");
    }
}

// --- Model Validators ---
public class CreateModelRequestValidator : AbstractValidator<CreateModelRequest>
{
    public CreateModelRequestValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty().WithMessage("ProviderId is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Model Name is required.");
        RuleFor(x => x.DeploymentName).NotEmpty().WithMessage("DeploymentName is required.");
        RuleFor(x => x.InputTokenPricePerMillion).GreaterThanOrEqualTo(0).WithMessage("Input price must be non-negative.");
        RuleFor(x => x.OutputTokenPricePerMillion).GreaterThanOrEqualTo(0).WithMessage("Output price must be non-negative.");
        RuleFor(x => x.ContextWindow).GreaterThan(0).WithMessage("ContextWindow must be positive.");
    }
}

public class UpdateModelRequestValidator : AbstractValidator<UpdateModelRequest>
{
    public UpdateModelRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Model Name is required.");
        RuleFor(x => x.DeploymentName).NotEmpty().WithMessage("DeploymentName is required.");
        RuleFor(x => x.InputTokenPricePerMillion).GreaterThanOrEqualTo(0).WithMessage("Input price must be non-negative.");
        RuleFor(x => x.OutputTokenPricePerMillion).GreaterThanOrEqualTo(0).WithMessage("Output price must be non-negative.");
        RuleFor(x => x.ContextWindow).GreaterThan(0).WithMessage("ContextWindow must be positive.");
    }
}

// --- Routing Rule Validators ---
public class CreateRoutingRuleRequestValidator : AbstractValidator<CreateRoutingRuleRequest>
{
    public CreateRoutingRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rule Name is required.");
        RuleFor(x => x.Priority).GreaterThan(0).WithMessage("Priority must be greater than zero.");
        RuleFor(x => x.ConditionsJson).NotEmpty().WithMessage("Conditions JSON is required.");
    }
}

public class UpdateRoutingRuleRequestValidator : AbstractValidator<UpdateRoutingRuleRequest>
{
    public UpdateRoutingRuleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Rule Name is required.");
        RuleFor(x => x.Priority).GreaterThan(0).WithMessage("Priority must be greater than zero.");
        RuleFor(x => x.ConditionsJson).NotEmpty().WithMessage("Conditions JSON is required.");
    }
}

// --- Budget Validators ---
public class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).WithMessage("MonthlyLimit must be greater than zero.");
        RuleFor(x => x.WarningThresholdPercent).InclusiveBetween(10m, 100m).WithMessage("WarningThresholdPercent must be between 10% and 100%.");
    }
}

public class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).WithMessage("MonthlyLimit must be greater than zero.");
        RuleFor(x => x.WarningThresholdPercent).InclusiveBetween(10m, 100m).WithMessage("WarningThresholdPercent must be between 10% and 100%.");
    }
}

// --- API Key Validators ---
public class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.ClientApplicationId).NotEmpty().WithMessage("ClientApplicationId is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.ExpiresAtUtc)
            .Must(expires => !expires.HasValue || expires.Value > DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future.");
    }
}

// --- Application Validators ---
public class CreateApplicationRequestValidator : AbstractValidator<CreateApplicationRequest>
{
    public CreateApplicationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Application Name is required.");
    }
}
