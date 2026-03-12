namespace GoogleClass.Models;

public class Comment : BaseEntityWithId
{
    public string? Text { get; set; }
    public bool IsDeleted { get; set; } = false;
    
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public Guid CommentableId { get; set; }
    public virtual Commentable? Commentable { get; set; }
    
    public Guid? ParentCommentId { get; set; }
    public virtual Comment? ParentComment { get; set; }
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
}