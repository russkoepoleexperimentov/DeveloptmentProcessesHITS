using System.ComponentModel.DataAnnotations;
using GoogleClass.Common;
using GoogleClass.DTOs.Common;

namespace GoogleClass.DTOs.Course;

public class CreatePostRequestDto
{
    [Required]
    [RegularExpression("post|task")]
    public required PostType Type { get; set; }

    [Required] public string Title { get; set; } = null!;

    [Required]
    public required string Text { get; set; } = null!;

    public DateTime? Deadline { get; set; }

    public int MaxScore { get; set; } = Constants.MAX_SCORE;
    
    [Required]
    public TaskType TaskType { get; set; }
    
    [Required]
    public bool SolvableAfterDeadline { get; set; }
    
    public List<Guid>? Files { get; set; }
}