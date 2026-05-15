using Domain.Models.Criteria;
using GoogleClass.DTOs.Common;

namespace GoogleClass.Models;

public class Assignment : GenericPost
{
    public TaskType TaskType { get; set; }
    public DateTime? Deadline { get; set; }
    public uint MaxScore { get; set; }
    public bool SolvableAfterDeadline { get; set; }

    public float? FailThreshold { get; set; }
    public float? SuccessThreshold { get; set; }
    public float StudentScoreWeight { get; set; }
    public float? PenaltyPerDay { get; set; }
    public int MaxDays { get; set; }
}
