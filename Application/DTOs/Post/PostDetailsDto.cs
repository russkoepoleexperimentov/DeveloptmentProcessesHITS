using System.ComponentModel.DataAnnotations;
using Application.DTOs.Criterion;
using Application.DTOs.Post;
using GoogleClass.DTOs.Common;

namespace GoogleClass.DTOs.Post;

public class PostDetailsDto
{
    public Guid Id { get; set; }

    [Required]
    public PostType Type { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Text { get; set; } = null!;

    public DateTime? Deadline { get; set; } = null;

    public int? MaxScore { get; set; } = null;

    public TaskType? TaskType { get; set; } = null;

    public bool? SolvableAfterDeadline { get; set; } = null;

    public List<FileDto>? Files { get; set; }

    public UserSolutionDto? UserSolution { get; set; } = null;

    public int? MinTeamSize { get; set; } = null;
    public int? MaxTeamSize { get; set; } = null;
    public TeamSolutionDto? TeamSolution { get; set; } = null;

    public CaptainSelectionMode? CaptainMode { get; set; }
    public int? VotingDurationHours { get; set; }

    public int? PredefinedTeamsCount { get; set; }
    public bool? AllowJoinTeam { get; set; }
    public bool? AllowLeaveTeam { get; set; }
    public bool? AllowStudentTransferCaptain { get; set; }

    public float? FailThreshold { get; set; }
    public float? SuccessThreshold { get; set; }
    public float? StudentScoreWeight { get; set; }
    public float? PenaltyPerDay { get; set; }
    public int? MaxDays { get; set; }

    public GradingMode? GradingMode { get; set; }
    public int? MinPeerReviewsRequired { get; set; }

    public List<CriterionDto>? Criteria { get; set; }
}
