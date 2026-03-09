namespace GoogleClass.Models;

public class FileSolution : BaseEntityWithId
{
    public Guid FileId { get; set; }
    public virtual UserFile File { get; set; } = null!;
    public Guid SolutionId { get; set; }
    public virtual Solution Solution { get; set; } = null!;
}