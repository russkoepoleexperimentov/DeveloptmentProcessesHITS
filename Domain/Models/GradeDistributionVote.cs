// Domain/Models/GradeDistributionVote.cs
using GoogleClass.Models;

namespace Domain.Models;

public enum GradeVoteType
{
    For,
    Against
}

public class GradeDistributionVote : BaseEntityWithId
{
    public Guid DistributionId { get; set; }
    public virtual GradeDistribution Distribution { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public GradeVoteType Vote { get; set; }
}