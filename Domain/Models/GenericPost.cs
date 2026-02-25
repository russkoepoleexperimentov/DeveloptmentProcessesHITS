namespace GoogleClass.Models;

public abstract class GenericPost : Post
{
    public ICollection<FilePost> FilePosts { get; set; } = new List<FilePost>();
}