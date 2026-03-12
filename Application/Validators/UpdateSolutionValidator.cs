using FluentValidation;
using GoogleClass.DTOs;
using GoogleClass.Models;

namespace Application.Validators;

public class UpdateSolutionRequestDtoValidator : AbstractValidator<UpdateSolutionRequestDto>
{
    public UpdateSolutionRequestDtoValidator()
    {
        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Score.HasValue);

        RuleFor(x => x.Status)
            .NotEqual(SolutionStatus.Pending)
            .WithMessage("Teacher must set Checked or Returned status");
    }
}