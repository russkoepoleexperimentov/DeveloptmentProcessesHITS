using Domain.Models.Criteria;

namespace GoogleClass.Models;

public class Solution : Commentable
{
    public string Text { get; set; } = null!;
    public uint Score { get; set; }

    public SolutionStatus Status { get; set; }

    public DateTime SubmittedAt { get; set; }

    public virtual ICollection<FileSolution> FileSolutions { get; set; } = new List<FileSolution>();
    public virtual User User { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public virtual Assignment? Task { get; set; }

    public virtual ICollection<WeightedCriterionValue> WeightedValues { get; set; } = new List<WeightedCriterionValue>();
    public virtual ICollection<ToggledCriterionValue> ToggledValues { get; set; } = new List<ToggledCriterionValue>();
}

public enum SolutionStatus
{
    Pending,
    Checked,
    Returned
}
