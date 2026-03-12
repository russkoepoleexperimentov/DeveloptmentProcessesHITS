using FluentValidation;
using GoogleClass.DTOs;

namespace Application.Validators;

public class SubmitSolutionRequestDtoValidator : AbstractValidator<SubmitSolutionRequestDto>
{
    public SubmitSolutionRequestDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || (x.Files != null && x.Files.Any()))
            .WithMessage("Solution must contain text or files");
    }
}