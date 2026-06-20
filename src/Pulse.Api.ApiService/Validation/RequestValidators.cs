using FluentValidation;
using Pulse.Api.ApiService.Contracts;

namespace Pulse.Api.ApiService.Validation;

public class SendMoodRequestValidator : AbstractValidator<SendMoodRequest>
{
    public SendMoodRequestValidator()
    {
        RuleFor(x => x.MoodType).IsInEnum();
    }
}

public class SendNeedRequestValidator : AbstractValidator<SendNeedRequest>
{
    public SendNeedRequestValidator()
    {
        RuleFor(x => x.NeedType).IsInEnum();
    }
}

public class SendThoughtRequestValidator : AbstractValidator<SendThoughtRequest>
{
    public SendThoughtRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Add a few words.")
            .MaximumLength(50).WithMessage("Keep it under 50 characters.");
    }
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
