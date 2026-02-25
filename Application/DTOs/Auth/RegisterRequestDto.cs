namespace GoogleClass.DTOs.Auth;

public class UserRegisterDto : UserLoginDto
{
    public string Credentials { get; set; } = null!;

}