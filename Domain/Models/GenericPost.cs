namespace GoogleClass.Models;

public abstract class GenericPost : Post
{
    public virtual ICollection<FilePost> FilePosts { get; set; } = new List<FilePost>();
}