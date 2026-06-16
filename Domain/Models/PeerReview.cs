using Domain.Models.Criteria;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;

namespace Domain.Models;

public class PeerReview : BaseEntityWithId
{
    public Guid TaskId { get; set; }
    public virtual GenericPost Task { get; set; } = null!;

    public Guid ReviewerId { get; set; }
    public virtual User Reviewer { get; set; } = null!;

    public Guid? ReviewerTeamId { get; set; }
    public virtual Team? ReviewerTeam { get; set; }

    public Guid? SolutionId { get; set; }
    public virtual Solution? Solution { get; set; }

    public Guid? TeamSolutionId { get; set; }
    public virtual TeamSolution? TeamSolution { get; set; }

    public PeerReviewStatus Status { get; set; }

    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual ICollection<WeightedCriterionValue> WeightedValues { get; set; } = new List<WeightedCriterionValue>();
    public virtual ICollection<ToggledCriterionValue> ToggledValues { get; set; } = new List<ToggledCriterionValue>();
}
