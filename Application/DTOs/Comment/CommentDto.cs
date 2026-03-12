using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Comment;

public class CommentDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public string Text { get; set; } = null!;
    
    [Required]
    public bool IsDeleted { get; set; }

    [Required]
    public CommentAuthorDto Author { get; set; } = null!;

    [Required]
    [Range(0, int.MaxValue)]
    public int NestedCount { get; set; }
}