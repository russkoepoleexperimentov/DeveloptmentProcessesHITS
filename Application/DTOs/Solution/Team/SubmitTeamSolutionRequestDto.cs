namespace GoogleClass.DTOs;

public class SubmitTeamSolutionRequestDto
{
    public string? Text { get; set; } = null;

    public List<Guid>? Files { get; set; }
}