namespace GoogleClass.DTOs.Course
{
    using GoogleClass.Models;

    namespace Application.DTOs.User
    {
        public class UserCourseDto
        {
            public Guid Id { get; set; }
            public string Title { get; set; } = null!;
            public UserRoleType Role { get; set; }
        }
    }
}
