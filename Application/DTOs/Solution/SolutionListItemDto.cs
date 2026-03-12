using System.ComponentModel.DataAnnotations;
using Application.DTOs.Post;
using GoogleClass.DTOs.User;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class SolutionListItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public UserCredentialsDto User { get; set; } = null!;

    [Required] 
    public string Text { get; set; } = null!;

    public int? Score { get; set; } = null;

    [Required]
    public SolutionStatus Status { get; set; }

    public List<FileDto>? Files { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}