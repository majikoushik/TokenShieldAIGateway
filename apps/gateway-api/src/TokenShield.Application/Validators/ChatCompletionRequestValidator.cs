using FluentValidation;
using TokenShield.Application.Dto;

namespace TokenShield.Application.Validators;

public class ChatCompletionRequestValidator : AbstractValidator<ChatCompletionRequest>
{
    private static readonly string[] ValidRoles = { "system", "user", "assistant" };

    public ChatCompletionRequestValidator()
    {
        RuleFor(x => x.Model)
            .NotEmpty()
            .WithMessage("Model parameter is required.");

        RuleFor(x => x.Messages)
            .NotEmpty()
            .WithMessage("Messages list cannot be empty.");

        RuleForEach(x => x.Messages)
            .ChildRules(msg =>
            {
                msg.RuleFor(m => m.Role)
                    .NotEmpty()
                    .Must(role => ValidRoles.Contains(role.ToLowerInvariant()))
                    .WithMessage("Message role must be either 'system', 'user', or 'assistant'.");

                msg.RuleFor(m => m.Content)
                    .NotEmpty()
                    .WithMessage("Message content cannot be empty.");
            });

        RuleFor(x => x.Stream)
            .Must(stream => stream != true)
            .WithMessage("Streaming responses are currently not supported in this version. Set 'stream' to false.");

        RuleFor(x => x.Temperature)
            .InclusiveBetween(0.0, 2.0)
            .When(x => x.Temperature.HasValue)
            .WithMessage("Temperature must be between 0.0 and 2.0.");

        RuleFor(x => x.MaxTokens)
            .GreaterThan(0)
            .When(x => x.MaxTokens.HasValue)
            .WithMessage("MaxTokens must be a positive integer.");
    }
}
