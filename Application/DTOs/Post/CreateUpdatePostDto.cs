using Application.DTOs.Criterion;
using GoogleClass.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Post
{
    public class CreateUpdatePostDto
    {
        [Required]
        public PostType Type { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTime? Deadline { get; set; }
        public int? MaxScore { get; set; }
        public TaskType? TaskType { get; set; }
        public bool? SolvableAfterDeadline { get; set; }
        public List<Guid>? Files { get; set; }

        public int? MinTeamSize { get; set; }
        public int? MaxTeamSize { get; set; }

        public CaptainSelectionMode? CaptainMode { get; set; }
        public int? VotingDurationHours { get; set; }

        public int? PredefinedTeamsCount { get; set; }
        public bool? AllowJoinTeam { get; set; }
        public bool? AllowLeaveTeam { get; set; }
        public bool? AllowStudentTransferCaptain { get; set; }

        public bool? CopyGroupsFromPreviousAssignment { get; set; }
        public Guid? SourceAssignmentId { get; set; }

        public float? FailThreshold { get; set; }
        public float? SuccessThreshold { get; set; }
        public float? StudentScoreWeight { get; set; }
        public float? PenaltyPerDay { get; set; }
        public int? MaxDays { get; set; }

        public GradingMode? GradingMode { get; set; }
        public int? MinPeerReviewsRequired { get; set; }

        public List<CriterionDefinitionDto>? Criteria { get; set; }
    }
}
