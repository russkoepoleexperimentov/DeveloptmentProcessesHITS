using Application.DTOs.Auth;
using FluentValidation;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.User;

namespace Application.Validators
{
    public class UserRegistrationValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegistrationValidator()
        {
            RuleFor(x => x.Credentials)
                .NotEmpty().WithMessage("Name field is required.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 7 characters long.");


        }
    }
    public class UserUpdateValidator : AbstractValidator<UserUpdateDto>
    {
        public UserUpdateValidator()
        {

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Invalid email format.");

        }
    }

    public class ChangePasswordValidator : AbstractValidator<UserChangePassword>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Password is required.")
                .MinimumLength(7).WithMessage("Password must be at least 7 characters long.");

            RuleFor(x => x.NewPassword).NotEmpty().WithMessage("Password is required.")
                .MinimumLength(7).WithMessage("Password must be at least 7 characters long.");
        }
    }

    public class UserLoginValidator : AbstractValidator<UserLoginDto>
    {
        public UserLoginValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Login is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}