using System.Net;

namespace GoogleClass.Models;

public class Course: BaseEntityWithId
{
    public string Title { get; set; } = null!;
    
    public Guid AuthorId { get; set; }
    public User Author { get; set; }
    
    public ICollection<CourseRole> CourseRoles { get; set; } = new List<CourseRole>();
}