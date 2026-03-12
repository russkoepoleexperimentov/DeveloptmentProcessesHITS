namespace GoogleClass.Models;

public class UserFile : BaseEntityWithId
{
    public string OriginalName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
    public virtual ICollection<FilePost> FilePosts { get; set; } = new List<FilePost>();
    public virtual ICollection<FileSolution> FileSolutions { get; set; } = new List<FileSolution>();
}