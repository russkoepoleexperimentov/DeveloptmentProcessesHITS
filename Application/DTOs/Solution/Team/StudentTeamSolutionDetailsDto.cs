using System.ComponentModel.DataAnnotations;
using Application.DTOs.Grading;
using Application.DTOs.Post;
using GoogleClass.DTOs.User;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class StudentTeamSolutionDetailsDto
{
    public Guid Id { get; set; }

    public string? Text { get; set; } = null;

    public List<FileDto>? Files { get; set; }

    public int? Score { get; set; } = null;

    [Required]
    public SolutionStatus Status { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }

    [Required]
    public TeamDto Team { get; set; } = null!;

    [Required]
    public UserCredentialsDto SubmittedBy { get; set; } = null!;

    public List<MemberSelfAssessmentDto>? SelfAssessments { get; set; }
    public EvaluationDto? TeacherEvaluation { get; set; }
    public GradeBreakdownDto? Breakdown { get; set; }
}

public class MemberSelfAssessmentDto
{
    public Guid UserId { get; set; }
    public string Credentials { get; set; } = string.Empty;
    public EvaluationDto Evaluation { get; set; } = new();
}
