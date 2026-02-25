using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs.Course;

public class ChangeRoleResponseDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public UserRoleType Role { get; set; }
}