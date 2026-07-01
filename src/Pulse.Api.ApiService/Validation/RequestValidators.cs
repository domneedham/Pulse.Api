using FluentValidation;
using Pulse.Api.ApiService.Contracts;

namespace Pulse.Api.ApiService.Validation;

public class SendMoodRequestValidator : AbstractValidator<SendMoodRequest>
{
    public SendMoodRequestValidator()
    {
        RuleFor(x => x.Text).PhrasePulse();
        RuleFor(x => x.Note).MaximumLength(80).WithMessage("A note can be up to 80 characters.");
    }
}

public class SendNeedRequestValidator : AbstractValidator<SendNeedRequest>
{
    public SendNeedRequestValidator()
    {
        RuleFor(x => x.Text).PhrasePulse();
        RuleFor(x => x.Note).MaximumLength(80).WithMessage("A note can be up to 80 characters.");
    }
}

public class SendThoughtRequestValidator : AbstractValidator<SendThoughtRequest>
{
    public SendThoughtRequestValidator()
    {
        RuleFor(x => x.Text).PhrasePulse();
        RuleFor(x => x.Note).MaximumLength(80).WithMessage("A note can be up to 80 characters.");
    }
}

public class AddFavoriteRequestValidator : AbstractValidator<AddFavoriteRequest>
{
    public AddFavoriteRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Text).PhrasePulse();
    }
}

internal static class PhraseRules
{
    /// <summary>A pulse / favourite phrase: non-empty, ≤ 80 chars.</summary>
    public static IRuleBuilderOptions<T, string> PhrasePulse<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Add a few words.")
            .MaximumLength(80).WithMessage("Keep it under 80 characters.");
}

public class AcceptInviteRequestValidator : AbstractValidator<AcceptInviteRequest>
{
    public AcceptInviteRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty().WithMessage("Enter the invite code your partner shared.")
            .MaximumLength(16);
    }
}
