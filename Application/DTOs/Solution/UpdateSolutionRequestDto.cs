using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs;

public class UpdateSolutionRequestDto
{
    public int? Score { get; set; } = null;

    [Required] public TaskStatus Status { get; set; }

    public string? Comment { get; set; } = null;
}