using Application.DTOs.Criterion;
using Application.DTOs.Post;

namespace Application.DTOs.Grading.PeerReview;

public class PeerReviewTargetDto
{
    public Guid ReviewId { get; set; }
    public Guid TaskId { get; set; }
    public AnonymizedSolutionDto Solution { get; set; } = null!;
    public List<CriterionDto> Criteria { get; set; } = new();
    public DateTime AssignedAt { get; set; }
}

public class AnonymizedSolutionDto
{
    public string? Text { get; set; }
    public List<FileDto>? Files { get; set; }
}
