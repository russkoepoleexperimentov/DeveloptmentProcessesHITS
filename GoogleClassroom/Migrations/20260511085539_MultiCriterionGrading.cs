using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class MultiCriterionGrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "FailThreshold",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxDays",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PenaltyPerDay",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Solution_SubmittedAt",
                table: "Commentable",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "StudentScoreWeight",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Commentable",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "SuccessThreshold",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TeamAssignment_FailThreshold",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamAssignment_MaxDays",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TeamAssignment_PenaltyPerDay",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TeamAssignment_StudentScoreWeight",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "TeamAssignment_SuccessThreshold",
                table: "Commentable",
                type: "real",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Criteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    CriterionType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Threshold = table.Column<float>(type: "real", nullable: true),
                    QualityCoefficient_Score = table.Column<float>(type: "real", nullable: true),
                    QualityCoefficient_Direction = table.Column<int>(type: "integer", nullable: true),
                    MaxAllowedScore = table.Column<float>(type: "real", nullable: true),
                    Score = table.Column<float>(type: "real", nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: true),
                    MaxScore = table.Column<float>(type: "real", nullable: true),
                    Weight = table.Column<float>(type: "real", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Criteria_Commentable_PostId",
                        column: x => x.PostId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ToggledCriterionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamSolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSelfAssessment = table.Column<bool>(type: "boolean", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ToggledCriterionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ToggledCriterionValues_AspNetUsers_EvaluatorUserId",
                        column: x => x.EvaluatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ToggledCriterionValues_Commentable_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToggledCriterionValues_Commentable_TeamSolutionId",
                        column: x => x.TeamSolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ToggledCriterionValues_Criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightedCriterionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeamSolutionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EvaluatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSelfAssessment = table.Column<bool>(type: "boolean", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightedCriterionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightedCriterionValues_AspNetUsers_EvaluatorUserId",
                        column: x => x.EvaluatorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeightedCriterionValues_Commentable_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeightedCriterionValues_Commentable_TeamSolutionId",
                        column: x => x.TeamSolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeightedCriterionValues_Criteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "Criteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_PostId",
                table: "Criteria",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ToggledCriterionValues_CriterionId",
                table: "ToggledCriterionValues",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToggledCriterionValues_EvaluatorUserId",
                table: "ToggledCriterionValues",
                column: "EvaluatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ToggledCriterionValues_SolutionId",
                table: "ToggledCriterionValues",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ToggledCriterionValues_TeamSolutionId",
                table: "ToggledCriterionValues",
                column: "TeamSolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionValues_CriterionId",
                table: "WeightedCriterionValues",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionValues_EvaluatorUserId",
                table: "WeightedCriterionValues",
                column: "EvaluatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionValues_SolutionId",
                table: "WeightedCriterionValues",
                column: "SolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightedCriterionValues_TeamSolutionId",
                table: "WeightedCriterionValues",
                column: "TeamSolutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ToggledCriterionValues");

            migrationBuilder.DropTable(
                name: "WeightedCriterionValues");

            migrationBuilder.DropTable(
                name: "Criteria");

            migrationBuilder.DropColumn(
                name: "FailThreshold",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "MaxDays",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "PenaltyPerDay",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Solution_SubmittedAt",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "StudentScoreWeight",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "SuccessThreshold",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_FailThreshold",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_MaxDays",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_PenaltyPerDay",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_StudentScoreWeight",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_SuccessThreshold",
                table: "Commentable");
        }
    }
}
