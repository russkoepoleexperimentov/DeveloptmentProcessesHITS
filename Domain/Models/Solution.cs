namespace GoogleClass.Models;

public class Solution : Commentable
{
    public string Text { get; set; } = null!;
    public uint Score { get; set; }
    
    public SolutionStatus Status { get; set; }
    
    public ICollection<FileSolution> FileSolutions { get; set; } = new List<FileSolution>();
    public User User { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public Assignment? Task { get; set; }
}

public enum SolutionStatus
{
    PendingCheck, 
    Checked,
    Returned
}