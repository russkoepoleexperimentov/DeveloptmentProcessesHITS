using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Course;

public class CreateCourseRequestDto
{
    [Required]
    public string Title { get; set; } = null!;
}