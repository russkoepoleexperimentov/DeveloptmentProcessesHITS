// Domain/Models/GradeDistributionEntry.cs
using GoogleClass.Models;

namespace Domain.Models;

public class GradeDistributionEntry : BaseEntityWithId
{
    public Guid DistributionId { get; set; }
    public virtual GradeDistribution Distribution { get; set; } = null!;

    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public decimal Points { get; set; } // может быть дробным
}