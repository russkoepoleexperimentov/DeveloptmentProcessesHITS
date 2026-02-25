namespace GoogleClass.Models;

public class File : BaseEntityWithId
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public ICollection<FilePost> FilePosts { get; set; } = new List<FilePost>();
    public ICollection<FileSolution> FileSolutions { get; set; } = new List<FileSolution>();
}