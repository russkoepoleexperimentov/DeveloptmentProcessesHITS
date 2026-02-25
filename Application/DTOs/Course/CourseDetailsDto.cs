using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs.Course;

public class CourseDetailsDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public UserRoleType Role { get; set; }

    [Required]
    public Guid AuthorId { get; set; }

    [Required]
    public string InviteCode { get; set; } = null!;
}