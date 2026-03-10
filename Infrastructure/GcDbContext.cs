using Domain.Models;
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

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseRole> CourseRoles { get; set; }
    public DbSet<RegularPost> Posts { get; set; } 
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<Solution> Solutions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<FilePost> FilePosts { get; set; }
}