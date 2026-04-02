using FluentValidation;
using GoogleClass.DTOs;

namespace Application.Validators;

public class SubmitTeamSolutionValidator : AbstractValidator<SubmitTeamSolutionRequestDto>
{
    public SubmitTeamSolutionValidator()
    {
        RuleFor(x => x.Text)
            .MaximumLength(10000)
            .When(x => x.Text != null);

        RuleFor(x => x.Files)
            .Must(files => files == null || files.Count <= 10)
            .WithMessage("Maximum 10 files allowed");
    }
}

public class UpdateTeamSolutionValidator : AbstractValidator<UpdateTeamSolutionRequestDto>
{
    public UpdateTeamSolutionValidator()
    {
        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Score.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Comment)
            .MaximumLength(5000)
            .When(x => x.Comment != null);
    }
}