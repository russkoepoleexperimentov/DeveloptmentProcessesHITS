namespace GoogleClass.Models;

public class FileSolution : BaseEntityWithId
{
    public Guid FileId { get; set; }
    public File File { get; set; }
    public Guid SolutionId { get; set; }
    public Solution Solution { get; set; }
}