using Domain.Models;
using Domain.Models.Criteria;
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
    public DbSet<GradeDistribution> GradeDistributions { get; set; }
    public DbSet<GradeDistributionEntry> GradeDistributionEntries { get; set; }
    public DbSet<GradeDistributionVote> GradeDistributionVotes { get; set; }

    public DbSet<Criterion> Criteria { get; set; }
    public DbSet<WeightedCriterion> WeightedCriteria { get; set; }
    public DbSet<QualityCoefficient> QualityCoefficients { get; set; }
    public DbSet<ToggledCriterion> ToggledCriteria { get; set; }
    public DbSet<BonusPenaltyCriterion> BonusPenaltyCriteria { get; set; }
    public DbSet<BlockingModifier> BlockingModifiers { get; set; }
    public DbSet<WeightedCriterionValue> WeightedCriterionValues { get; set; }
    public DbSet<ToggledCriterionValue> ToggledCriterionValues { get; set; }
    public DbSet<PeerReview> PeerReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Assignment>()
            .Property(a => a.GradingMode)
            .HasDefaultValue(GoogleClass.DTOs.Common.GradingMode.TeacherReview);
        modelBuilder.Entity<TeamAssignment>()
            .Property(a => a.GradingMode)
            .HasDefaultValue(GoogleClass.DTOs.Common.GradingMode.TeacherReview);
        modelBuilder.Entity<Solution>()
            .Property(s => s.PeerReviewCounted)
            .HasDefaultValue(false);

        modelBuilder.Entity<Criterion>()
            .HasDiscriminator<string>("CriterionType")
            .HasValue<WeightedCriterion>("Weighted")
            .HasValue<QualityCoefficient>("Quality")
            .HasValue<BonusPenaltyCriterion>("BonusPenalty")
            .HasValue<BlockingModifier>("Blocking");

        modelBuilder.Entity<Criterion>()
            .HasOne(c => c.Post)
            .WithMany(p => p.Criteria)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WeightedCriterionValue>(b =>
        {
            b.HasOne(v => v.Criterion)
                .WithMany()
                .HasForeignKey(v => v.CriterionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.Solution)
                .WithMany(s => s.WeightedValues)
                .HasForeignKey(v => v.SolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.TeamSolution)
                .WithMany(s => s.WeightedValues)
                .HasForeignKey(v => v.TeamSolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.Evaluator)
                .WithMany()
                .HasForeignKey(v => v.EvaluatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(v => v.PeerReview)
                .WithMany(p => p.WeightedValues)
                .HasForeignKey(v => v.PeerReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToggledCriterionValue>(b =>
        {
            b.HasOne(v => v.Criterion)
                .WithMany()
                .HasForeignKey(v => v.CriterionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.Solution)
                .WithMany(s => s.ToggledValues)
                .HasForeignKey(v => v.SolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.TeamSolution)
                .WithMany(s => s.ToggledValues)
                .HasForeignKey(v => v.TeamSolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(v => v.Evaluator)
                .WithMany()
                .HasForeignKey(v => v.EvaluatorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(v => v.PeerReview)
                .WithMany(p => p.ToggledValues)
                .HasForeignKey(v => v.PeerReviewId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PeerReview>(b =>
        {
            b.HasOne(p => p.Task)
                .WithMany()
                .HasForeignKey(p => p.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Reviewer)
                .WithMany()
                .HasForeignKey(p => p.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.ReviewerTeam)
                .WithMany()
                .HasForeignKey(p => p.ReviewerTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Solution)
                .WithMany()
                .HasForeignKey(p => p.SolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.TeamSolution)
                .WithMany()
                .HasForeignKey(p => p.TeamSolutionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(p => new { p.TaskId, p.ReviewerId, p.SolutionId }).IsUnique();
            b.HasIndex(p => new { p.TaskId, p.ReviewerId, p.TeamSolutionId }).IsUnique();
        });
    }
}
