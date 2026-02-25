using Microsoft.AspNetCore.Identity;

namespace GoogleClass.Models;

public class User : IdentityUser<Guid>
{
    public string Credentials { get; set; } = null!;
    //public ICollection<CourseRole> CourseRoles { get; set; } = new List<CourseRole>();
    //public ICollection<Solution> Solutions { get; set; } = new List<Solution>();
}