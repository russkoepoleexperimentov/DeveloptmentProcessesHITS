using Domain.Models;
using Domain.Models.Domain.Models;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO.Compression;

public class GcDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public GcDbContext(DbContextOptions<GcDbContext> options) : base(options)
    {
    }

    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<CaptainVote> CaptainVotes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseRole> CourseRoles { get; set; }
    public DbSet<RegularPost> Posts { get; set; } 
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<TeamAssignment> TeamAssignments { get; set; }
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<Solution> Solutions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<FileSolution> FileSolutions { get; set; }
    public DbSet<FilePost> FilePosts { get; set; }
    public DbSet<TeamSolution> TeamSolutions { get; set; }
    public DbSet<FileTeamSolution> FileTeamSolutions { get; set; }
    public DbSet<Team> Teams { get; set; }
}