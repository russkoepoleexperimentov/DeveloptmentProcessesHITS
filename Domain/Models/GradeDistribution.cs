// Domain/Models/GradeDistribution.cs
using GoogleClass.Models;

namespace Domain.Models;

public class GradeDistribution : BaseEntityWithId
{
    public Guid TeamId { get; set; }
    public virtual Team Team { get; set; } = null!;

    public Guid AssignmentId { get; set; }
    public virtual TeamAssignment Assignment { get; set; } = null!;

    // Сумма raw-оценки команды (берётся из TeamSolution.Score)
    public uint RawScore { get; set; }

    // Флаг, изменялось ли распределение от равного по умолчанию
    public bool IsCustomized { get; set; }

    public virtual ICollection<GradeDistributionEntry> Entries { get; set; } = new List<GradeDistributionEntry>();
    public virtual ICollection<GradeDistributionVote> Votes { get; set; } = new List<GradeDistributionVote>();
}