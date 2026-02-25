using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs.Course;

public class JoinCourseResponseDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public UserRoleType Role { get; set; }
}