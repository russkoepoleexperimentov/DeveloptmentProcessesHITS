namespace GoogleClass.Models;

public class FilePost : BaseEntityWithId
{
    public Guid FileId { get; set; }
    public File File { get; set; }
    public Guid PostId { get; set; }
    public GenericPost Post { get; set; }
}