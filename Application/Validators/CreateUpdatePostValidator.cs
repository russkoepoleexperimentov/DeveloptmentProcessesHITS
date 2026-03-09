using Application.DTOs.Post;
using FluentValidation;
using GoogleClass.DTOs.Common;

public class CreateUpdatePostValidator : AbstractValidator<CreateUpdatePostDto>
{
    public CreateUpdatePostValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();

        When(x => x.Type == PostType.TASK, () =>
        {
            RuleFor(x => x.Deadline).NotNull().WithMessage("Deadline is required for task");
            RuleFor(x => x.MaxScore).InclusiveBetween(1, 100);
            RuleFor(x => x.TaskType).NotNull().WithMessage("TaskType is required for task")
                .Must(t => t == TaskType.Mandatory || t == TaskType.Optional);
            RuleFor(x => x.SolvableAfterDeadline).NotNull();
        });

        When(x => x.Type == PostType.POST, () =>
        {
            RuleFor(x => x.Deadline).Null();
            RuleFor(x => x.MaxScore).Null();
            RuleFor(x => x.TaskType).Null();
            RuleFor(x => x.SolvableAfterDeadline).Null();
        });
    }
}