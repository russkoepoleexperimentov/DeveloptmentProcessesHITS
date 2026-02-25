using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs.Course;

public class CourseMemberDto
{
    [Required]
    public Guid Id { get; set; }

    [Required] 
    public string Credentials { get; set; } = null!;

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public UserRoleType Role { get; set; }
}