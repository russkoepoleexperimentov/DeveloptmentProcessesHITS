using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Course;

public class CreateUpdateCourseResponseDto
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public string Title { get; set; } = null!;
}