namespace Application.DTOs.Grading.PeerReview;

public class SubmitPeerReviewDto
{
    public EvaluationDto Evaluation { get; set; } = new();
}
