using GoogleClass.Models;

namespace Domain.Models.Criteria;

public class ToggledCriterionValue : BaseEntityWithId
{
    public Guid CriterionId { get; set; }
    public virtual ToggledCriterion Criterion { get; set; } = null!;

    public Guid? SolutionId { get; set; }
    public virtual Solution? Solution { get; set; }

    public Guid? TeamSolutionId { get; set; }
    public virtual TeamSolution? TeamSolution { get; set; }

    public Guid EvaluatorUserId { get; set; }
    public virtual User Evaluator { get; set; } = null!;

    public bool IsSelfAssessment { get; set; }

    public bool Enabled { get; set; }
}
