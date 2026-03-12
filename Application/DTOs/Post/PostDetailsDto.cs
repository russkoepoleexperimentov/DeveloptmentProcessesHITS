using System.ComponentModel.DataAnnotations;
using Application.DTOs.Post;
using GoogleClass.DTOs.Common;

namespace GoogleClass.DTOs.Post;

public class PostDetailsDto
{
    public Guid Id { get; set; }

    [Required]
    public PostType Type { get; set; }

    [Required]
    public string Title { get; set; } = null!;
    
    [Required]
    public string Text { get; set; } = null!;
    
    public DateTime? Deadline { get; set; } = null;

    public int? MaxScore { get; set; } = null;

    public TaskType? TaskType { get; set; } = null;

    public bool? SolvableAfterDeadline { get; set; } = null;

    public List<FileDto>? Files { get; set; }

    public UserSolutionDto? UserSolution { get; set; } = null;
}