namespace GoogleClass.Models;

public class Assignment : GenericPost
{
    public DateTime? Deadline { get; set; }
    public uint MaxScore { get; set; }
    public bool SolvableAfterDeadline { get; set; }
}