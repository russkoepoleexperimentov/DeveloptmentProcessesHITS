using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.User;

public class UserDto
{
    [Required] public string Credentials { get; set; } = null!;

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public Guid Id { get; set; }
}