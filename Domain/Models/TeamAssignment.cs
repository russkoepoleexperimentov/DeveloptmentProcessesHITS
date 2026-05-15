using Domain.Models.Criteria;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;

namespace Domain.Models;

public class TeamAssignment : GenericPost
{
    public DateTime? Deadline { get; set; }
    public uint MaxScore { get; set; }
    public bool SolvableAfterDeadline { get; set; }
    public int MinTeamSize { get; set; } = 2;
    public int MaxTeamSize { get; set; } = 5;

    public CaptainSelectionMode CaptainMode { get; set; } = CaptainSelectionMode.FirstMember;
    public Guid? FixedCaptainId { get; set; }
    public int? VotingDurationHours { get; set; }

    public int PredefinedTeamsCount { get; set; } = 0;

    public bool AllowJoinTeam { get; set; } = true;
    public bool AllowLeaveTeam { get; set; } = true;
    public bool AllowStudentTransferCaptain { get; set; } = true;
    public bool CopyGroupsFromPreviousAssignment { get; set; }
    public Guid? SourceAssignmentId { get; set; }

    public float? FailThreshold { get; set; }
    public float? SuccessThreshold { get; set; }
    public float StudentScoreWeight { get; set; }
    public float? PenaltyPerDay { get; set; }
    public int MaxDays { get; set; }

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
    public virtual ICollection<TeamSolution> TeamSolutions { get; set; } = new List<TeamSolution>();
}
