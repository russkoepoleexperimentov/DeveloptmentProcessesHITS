using Domain.Models;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class GcDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public GcDbContext(DbContextOptions<GcDbContext> options) : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseRole> CourseRoles { get; set; }
}