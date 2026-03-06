using System.Net;

namespace GoogleClass.Models;

public class Course: BaseEntityWithId
{
    public string Title { get; set; } = null!;
    
    public Guid AuthorId { get; set; }
    public string InviteCode { get; set; } = null!;

    public virtual User Author { get; set; } = null!;
    public virtual ICollection<CourseRole> CourseRoles { get; set; } = null!;
}