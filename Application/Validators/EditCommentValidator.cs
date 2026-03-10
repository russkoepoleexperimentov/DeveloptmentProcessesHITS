using FluentValidation;
using GoogleClass.DTOs.Comment;

namespace Application.Validators;

public class EditCommentValidator : AbstractValidator<EditCommentRequestDto>
{
    public EditCommentValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(200);
    }
}