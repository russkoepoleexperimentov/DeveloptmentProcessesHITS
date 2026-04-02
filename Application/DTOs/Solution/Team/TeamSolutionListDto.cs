using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs;

public class TeamSolutionListDto
{
    [Required]
    public List<TeamSolutionListItemDto> Records { get; set; } = new();
    
    [Required]
    public int TotalRecords { get; set; }
}