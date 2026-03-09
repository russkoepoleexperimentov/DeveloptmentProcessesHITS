namespace GoogleClass.Models;

public class FilePost : BaseEntityWithId
{
    public Guid FileId { get; set; }
    public virtual UserFile File { get; set; } = null!;
    public Guid PostId { get; set; }
    public virtual GenericPost Post { get; set; } = null!;
}