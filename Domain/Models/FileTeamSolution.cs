using GoogleClass.Models;

namespace Domain.Models;

public class FileTeamSolution : BaseEntityWithId
{
    public Guid FileId { get; set; }
    public virtual UserFile File { get; set; } = null!;

    public Guid TeamSolutionId { get; set; }
    public virtual TeamSolution TeamSolution { get; set; } = null!;
}