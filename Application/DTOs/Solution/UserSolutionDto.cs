using System.ComponentModel.DataAnnotations;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class UserSolutionDto
{
    public Guid Id { get; set; }

    [Required] 
    public String Text { get; set; } = null!;
    
    [Required]
    public uint Score { get; set; }
    
    public SolutionStatus Status { get; set; }
}