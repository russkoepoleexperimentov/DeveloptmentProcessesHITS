namespace GoogleClass.Models;

public abstract class Post : Commentable
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Course Course { get; set; }
    public Guid CourseId { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; }
}