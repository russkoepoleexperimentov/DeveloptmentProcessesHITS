using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs;

public class StudentSolutionDetailsDto
{
    public string? Text { get; set; } = null;

    public List<Guid>? Files { get; set; }

    public int? Score { get; set; } = null;

    [Required]
    public TaskStatus Status { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}