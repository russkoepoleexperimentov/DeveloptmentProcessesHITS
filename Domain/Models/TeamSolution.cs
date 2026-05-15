using Domain.Models.Criteria;
using GoogleClass.Models;

namespace Domain.Models;

public class TeamSolution : Commentable
{
    public string Text { get; set; } = string.Empty;
    public uint Score { get; set; }
    public SolutionStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }

    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public Guid SubmittedByUserId { get; set; }
    public virtual User SubmittedByUser { get; set; } = null!;

    public Guid TaskId { get; set; }
    public virtual TeamAssignment Task { get; set; } = null!;

    public virtual ICollection<FileTeamSolution> FileTeamSolutions { get; set; } = new List<FileTeamSolution>();

    public virtual ICollection<WeightedCriterionValue> WeightedValues { get; set; } = new List<WeightedCriterionValue>();
    public virtual ICollection<ToggledCriterionValue> ToggledValues { get; set; } = new List<ToggledCriterionValue>();
}
