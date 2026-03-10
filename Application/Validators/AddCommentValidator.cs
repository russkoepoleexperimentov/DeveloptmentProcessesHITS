using FluentValidation;
using GoogleClass.DTOs.Comment;

namespace Application.Validators;

public class AddCommentValidator : AbstractValidator<AddCommentRequestDto>
{
    public AddCommentValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200);
    }
}