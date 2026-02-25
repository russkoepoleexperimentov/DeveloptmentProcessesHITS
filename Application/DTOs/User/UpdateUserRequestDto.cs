using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.User;

public class UserUpdateDto
{
    public string Credentials { get; set; } = null!;
    public string Email { get; set; } = null!;

}