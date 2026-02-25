using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.User;

public class UserCredentialsDto
{
    [Required] public string Credentials { get; set; } = null!;

    [Required]
    public Guid Id { get; set; }
}