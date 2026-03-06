using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Course;

public class CreateUpdateCourseRequestDto
{
    [Required]
    public string Title { get; set; } = null!;
}