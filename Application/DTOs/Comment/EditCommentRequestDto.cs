using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Comment;

public class EditCommentRequestDto
{
    [Required]
    public required string Text { get; set; }
}