using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class GradeDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GradeDistributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RawScore = table.Column<long>(type: "bigint", nullable: false),
                    IsCustomized = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDistributions_Commentable_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeDistributions_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeDistributionEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDistributionEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDistributionEntries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeDistributionEntries_GradeDistributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "GradeDistributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeDistributionVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Vote = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeDistributionVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeDistributionVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeDistributionVotes_GradeDistributions_DistributionId",
                        column: x => x.DistributionId,
                        principalTable: "GradeDistributions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributionEntries_DistributionId",
                table: "GradeDistributionEntries",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributionEntries_UserId",
                table: "GradeDistributionEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributions_AssignmentId",
                table: "GradeDistributions",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributions_TeamId",
                table: "GradeDistributions",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributionVotes_DistributionId",
                table: "GradeDistributionVotes",
                column: "DistributionId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeDistributionVotes_UserId",
                table: "GradeDistributionVotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeDistributionEntries");

            migrationBuilder.DropTable(
                name: "GradeDistributionVotes");

            migrationBuilder.DropTable(
                name: "GradeDistributions");
        }
    }
}
