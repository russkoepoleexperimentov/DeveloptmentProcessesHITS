using GoogleClass.Models;

namespace Domain.Models;

public class TeamAssignment : GenericPost
{
    public DateTime? Deadline { get; set; }
    public uint MaxScore { get; set; }
    public bool SolvableAfterDeadline { get; set; }
    
    public int MinTeamSize { get; set; } = 2;
    public int MaxTeamSize { get; set; } = 5;
    
    public virtual ICollection<TeamSolution> TeamSolutions { get; set; } = new List<TeamSolution>();
}