namespace Application.DTOs.Grading.PeerReview;

public class PeerReviewTeamTargetDto
{
    public Guid TeamSolutionId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public bool AlreadyReviewed { get; set; }
}
