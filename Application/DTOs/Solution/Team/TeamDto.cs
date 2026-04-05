using System.ComponentModel.DataAnnotations;
using Domain.Models;
using GoogleClass.Models;

namespace GoogleClass.DTOs;

public class TeamDto
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public List<TeamMemberDto> Members { get; set; } = new();
}

public class TeamMemberDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public string Credentials { get; set; } = string.Empty;
    
    [Required]
    public TeamMemberRole Role { get; set; }
}

public class RenameTeamRequestDto
{
    public string NewName { get; set; } = string.Empty;
}