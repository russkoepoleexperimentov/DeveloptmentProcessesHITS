using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs.Course;

public class ChangeRoleRequestDto
{
    [Required]
    public UserRoleType Role { get; set; }
}