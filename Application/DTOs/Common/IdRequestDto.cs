using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Common;

public class IdRequestDto
{
    [Required]
    public Guid Id { get; set; }
}