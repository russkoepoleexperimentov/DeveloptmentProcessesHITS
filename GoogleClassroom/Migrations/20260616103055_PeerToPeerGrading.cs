using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class PeerToPeerGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PeerReviewId",
                table: "WeightedCriterionValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PeerReviewId",
                table: "ToggledCriterionValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GradingMode",
                table: "Commentable",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinPeerReviewsRequired",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PeerReviewCounted",
                table: "Commentable",
                type: "boolean",
                nullable: true,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TeamAssignment_GradingMode",
                table: "Commentable",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PeerReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerTeamId = table.Column<Guid>(type: "uuid", nullable: true),
                    SolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamSolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeerReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeerReviews_AspNetUsers_ReviewerId",
                        column: x => x.ReviewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PeerReviews_Commentable_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeerReviews_Commentable_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeerReviews_Commentable_TeamSolutionId",
                        column: x => x.TeamSolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeerReviews_Teams_ReviewerTeamId",
                        column: x => x.ReviewerTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionValues_PeerReviewId",
                table: "WeightedCriterionValues",
                column: "PeerReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ToggledCriterionValues_PeerReviewId",
                table: "ToggledCriterionValues",
                column: "PeerReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_ReviewerId",
                table: "PeerReviews",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_ReviewerTeamId",
                table: "PeerReviews",
                column: "ReviewerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_SolutionId",
                table: "PeerReviews",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_TaskId_ReviewerId_SolutionId",
                table: "PeerReviews",
                columns: new[] { "TaskId", "ReviewerId", "SolutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_TaskId_ReviewerId_TeamSolutionId",
                table: "PeerReviews",
                columns: new[] { "TaskId", "ReviewerId", "TeamSolutionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeerReviews_TeamSolutionId",
                table: "PeerReviews",
                column: "TeamSolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ToggledCriterionValues_PeerReviews_PeerReviewId",
                table: "ToggledCriterionValues",
                column: "PeerReviewId",
                principalTable: "PeerReviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WeightedCriterionValues_PeerReviews_PeerReviewId",
                table: "WeightedCriterionValues",
                column: "PeerReviewId",
                principalTable: "PeerReviews",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ToggledCriterionValues_PeerReviews_PeerReviewId",
                table: "ToggledCriterionValues");

            migrationBuilder.DropForeignKey(
                name: "FK_WeightedCriterionValues_PeerReviews_PeerReviewId",
                table: "WeightedCriterionValues");

            migrationBuilder.DropTable(
                name: "PeerReviews");

            migrationBuilder.DropIndex(
                name: "IX_WeightedCriterionValues_PeerReviewId",
                table: "WeightedCriterionValues");

            migrationBuilder.DropIndex(
                name: "IX_ToggledCriterionValues_PeerReviewId",
                table: "ToggledCriterionValues");

            migrationBuilder.DropColumn(
                name: "PeerReviewId",
                table: "WeightedCriterionValues");

            migrationBuilder.DropColumn(
                name: "PeerReviewId",
                table: "ToggledCriterionValues");

            migrationBuilder.DropColumn(
                name: "GradingMode",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "MinPeerReviewsRequired",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "PeerReviewCounted",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_GradingMode",
                table: "Commentable");
        }
    }
}
