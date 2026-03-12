using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class StudentSolutionDetailsDto
{
    public string? Text { get; set; } = null;

    public List<Guid>? Files { get; set; }

    public int? Score { get; set; } = null;

    [Required]
    public SolutionStatus Status { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}