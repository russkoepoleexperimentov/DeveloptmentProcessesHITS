using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Course;

public class JoinCourseRequestDto
{
    [Required] 
    public string InviteCode { get; set; } = null!;
}