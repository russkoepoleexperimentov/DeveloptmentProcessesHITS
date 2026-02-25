namespace GoogleClass.DTOs;

public class SubmitSolutionRequestDto
{
    public string? Text { get; set; } = null;

    public List<Guid>? Files { get; set; }
}