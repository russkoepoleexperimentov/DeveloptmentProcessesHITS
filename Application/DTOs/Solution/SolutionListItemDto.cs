using System.ComponentModel.DataAnnotations;
using GoogleClass.DTOs.User;

namespace GoogleClass.DTOs;

public class SolutionListItemDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public UserCredentialsDto User { get; set; } = null!;

    [Required] 
    public string Text { get; set; } = null!;

    public int? Score { get; set; } = null;

    [Required]
    public string Status { get; set; } = null!;

    public List<Guid>? Files { get; set; }

    [Required]
    public DateTime UpdatedDate { get; set; }
}