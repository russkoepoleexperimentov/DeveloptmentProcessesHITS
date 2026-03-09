using GoogleClass.DTOs.Common;

namespace GoogleClass.Models;

public class Assignment : GenericPost
{
    public TaskType TaskType { get; set; }
    public DateTime? Deadline { get; set; }
    public uint MaxScore { get; set; }
    public bool SolvableAfterDeadline { get; set; }
}