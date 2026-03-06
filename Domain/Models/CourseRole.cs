namespace GoogleClass.Models;

public class CourseRole : BaseEntityWithId
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public UserRoleType RoleType { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;

}