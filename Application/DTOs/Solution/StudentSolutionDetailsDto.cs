using System.ComponentModel.DataAnnotations;
using Application.DTOs.Post;
using GoogleClass.DTOs.Post;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class StudentSolutionDetailsDto
{
    public Guid Id { get; set; }
    public string? Text { get; set; } = null;

    public List<FileDto>? Files { get; set; }

    public int? Score { get; set; } = null;

    [Required]
    public SolutionStatus Status { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}