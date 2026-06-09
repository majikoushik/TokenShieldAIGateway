using FluentValidation;
using TokenShield.Application.Dto.Admin;

namespace TokenShield.Application.Validators.Admin;

public class CreateProfilerRuleRequestValidator : AbstractValidator<CreateProfilerRuleRequest>
{
    public CreateProfilerRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.TargetTaskType)
            .NotEmpty().WithMessage("TargetTaskType is required.")
            .MaximumLength(100).WithMessage("TargetTaskType must not exceed 100 characters.");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1).WithMessage("Confidence must be between 0 and 1.");
            
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority must be non-negative.");
    }
}

public class UpdateProfilerRuleRequestValidator : AbstractValidator<UpdateProfilerRuleRequest>
{
    public UpdateProfilerRuleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.TargetTaskType)
            .NotEmpty().WithMessage("TargetTaskType is required.")
            .MaximumLength(100).WithMessage("TargetTaskType must not exceed 100 characters.");

        RuleFor(x => x.Confidence)
            .InclusiveBetween(0, 1).WithMessage("Confidence must be between 0 and 1.");
            
        RuleFor(x => x.Priority)
            .GreaterThanOrEqualTo(0).WithMessage("Priority must be non-negative.");
    }
}
