using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs;

public class SolutionListDto
{
    [Required]
    public List<SolutionListItemDto> Records { get; set; }
    
    [Required]
    public int TotalRecords { get; set; }
}