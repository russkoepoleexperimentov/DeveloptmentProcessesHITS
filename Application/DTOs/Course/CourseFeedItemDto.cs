using System.ComponentModel.DataAnnotations;
using GoogleClass.DTOs.Common;

namespace GoogleClass.DTOs.Course;

public class CourseFeedItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public PostType Type { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public DateTime CreatedDate { get; set; }
}