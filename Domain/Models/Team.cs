using Domain.Models;

namespace GoogleClass.Models
{
    public class Team : BaseEntityWithId
    {
        public string Name { get; set; } = string.Empty;
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;
        public Guid AssignmentId { get; set; } 
        public virtual TeamAssignment Assignment { get; set; } = null!;
        public Guid? FixedCaptainId { get; set; }

        public virtual ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
        public virtual ICollection<TeamSolution> TeamSolutions { get; set; } = new List<TeamSolution>();
    }

    public class TeamMember : BaseEntityWithId
    {
        public Guid TeamId { get; set; }
        public virtual Team Team { get; set; } = null!;
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TeamMemberRole
    {
        Member,
        Leader
    }
}