using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Comment;

public class CommentAuthorDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Credentials { get; set; } = null!;
}