using System.ComponentModel.DataAnnotations;
using Application.DTOs.Grading;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class UpdateTeamSolutionRequestDto
{
    public int? Score { get; set; } = null;

    [Required]
    public SolutionStatus Status { get; set; }

    public string? Comment { get; set; } = null;

    public EvaluationDto? Evaluation { get; set; }
}
