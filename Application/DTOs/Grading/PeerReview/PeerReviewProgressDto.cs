using GoogleClass.DTOs.Common;

namespace Application.DTOs.Grading.PeerReview;

public class PeerReviewProgressDto
{
    public GradingMode GradingMode { get; set; } = GradingMode.PeerToPeer;
    public int Required { get; set; }
    public int Completed { get; set; }
    public bool CanFinish { get; set; }
    public bool IsCounted { get; set; }
}
