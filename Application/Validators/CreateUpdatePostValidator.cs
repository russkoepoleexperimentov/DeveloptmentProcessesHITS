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

        When(x => x.Type == PostType.TEAM_TASK, () =>
        {
            RuleFor(x => x.MaxScore).InclusiveBetween(1, 100);
            RuleFor(x => x.MinTeamSize)
                .GreaterThanOrEqualTo(1)
                .When(x => x.MinTeamSize.HasValue);
            RuleFor(x => x.MaxTeamSize)
                .GreaterThanOrEqualTo(x => x.MinTeamSize ?? 1)
                .When(x => x.MaxTeamSize.HasValue)
                .WithMessage("MaxTeamSize must be >= MinTeamSize");
        });

        When(x => x.Type == PostType.TASK && x.GradingMode == GradingMode.PeerToPeer, () =>
        {
            RuleFor(x => x.MinPeerReviewsRequired)
                .NotNull()
                .GreaterThanOrEqualTo(1)
                .WithMessage("MinPeerReviewsRequired is required and must be >= 1 for PeerToPeer individual task");
        });

        When(x => x.Type == PostType.POST, () =>
        {
            RuleFor(x => x.Deadline).Null();
            RuleFor(x => x.MaxScore).Null();
            RuleFor(x => x.TaskType).Null();
            RuleFor(x => x.SolvableAfterDeadline).Null();
        });

        When(x => x.Type == PostType.TASK || x.Type == PostType.TEAM_TASK, () =>
        {
            RuleFor(x => x.FailThreshold)
                .InclusiveBetween(0f, 1f)
                .When(x => x.FailThreshold.HasValue);
            RuleFor(x => x.SuccessThreshold)
                .InclusiveBetween(0f, 1f)
                .When(x => x.SuccessThreshold.HasValue);
            RuleFor(x => x.StudentScoreWeight)
                .InclusiveBetween(0f, 1f)
                .When(x => x.StudentScoreWeight.HasValue);
            RuleFor(x => x.PenaltyPerDay)
                .GreaterThan(0f)
                .When(x => x.PenaltyPerDay.HasValue);
            RuleFor(x => x.MaxDays)
                .GreaterThan(0)
                .When(x => x.PenaltyPerDay.HasValue)
                .WithMessage("MaxDays must be > 0 when penaltyPerDay is set");
        });
    }
}
