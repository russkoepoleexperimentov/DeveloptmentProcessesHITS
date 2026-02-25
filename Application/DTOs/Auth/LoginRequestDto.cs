using System.ComponentModel.DataAnnotations;
using GoogleClass.Common;

namespace GoogleClass.DTOs.Auth;

public class UserLoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(Constants.MIN_PASSWORD_LENGTH)]
    [MaxLength(Constants.MAX_PASSWORD_LENGTH)]
    public string Password { get; set; } = null!;
}