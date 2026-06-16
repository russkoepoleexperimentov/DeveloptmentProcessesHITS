using Application.DTOs.Grading.PeerReview;
using FluentValidation;

namespace Application.Validators;

public class SubmitPeerReviewDtoValidator : AbstractValidator<SubmitPeerReviewDto>
{
    public SubmitPeerReviewDtoValidator()
    {
        RuleFor(x => x.Evaluation).NotNull();

        When(x => x.Evaluation != null, () =>
        {
            RuleForEach(x => x.Evaluation.WeightedValues).ChildRules(v =>
            {
                v.RuleFor(w => w.CriterionId).NotEmpty();
                v.RuleFor(w => w.Score).GreaterThanOrEqualTo(0f);
            });

            RuleForEach(x => x.Evaluation.ToggledValues).ChildRules(v =>
            {
                v.RuleFor(t => t.CriterionId).NotEmpty();
            });
        });
    }
}
