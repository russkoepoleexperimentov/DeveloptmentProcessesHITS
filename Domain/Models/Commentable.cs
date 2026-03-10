namespace GoogleClass.Models;

public abstract class Commentable : BaseEntityWithId
{
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
}