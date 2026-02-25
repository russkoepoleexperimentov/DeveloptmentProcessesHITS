namespace GoogleClass.Models;

public class Comment : BaseEntityWithId
{
    public string? Text { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid CommentableId { get; set; }
    public Commentable? Commentable { get; set; }
    
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}