using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Comment;

public class AddCommentRequestDto
{
    [Required] public string Text { get; set; } = null!;
}