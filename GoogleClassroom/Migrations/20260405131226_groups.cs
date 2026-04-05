using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class groups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Commentable",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<bool>(
                name: "AllowJoinTeam",
                table: "Commentable",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowLeaveTeam",
                table: "Commentable",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowStudentTransferCaptain",
                table: "Commentable",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CaptainMode",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CopyGroupsFromPreviousAssignment",
                table: "Commentable",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FixedCaptainId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTeamSize",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinTeamSize",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PredefinedTeamsCount",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Solution_Score",
                table: "Commentable",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Solution_Status",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Solution_TaskId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceAssignmentId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedByUserId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TeamAssignment_Deadline",
                table: "Commentable",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TeamAssignment_MaxScore",
                table: "Commentable",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TeamAssignment_SolvableAfterDeadline",
                table: "Commentable",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamSolution_Text",
                table: "Commentable",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VotingDurationHours",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileTeamSolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamSolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTeamSolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTeamSolutions_Commentable_TeamSolutionId",
                        column: x => x.TeamSolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileTeamSolutions_UserFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UserFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Commentable_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Teams_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaptainVotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    VoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptainVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaptainVotes_AspNetUsers_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaptainVotes_AspNetUsers_VoterId",
                        column: x => x.VoterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaptainVotes_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commentable_Solution_TaskId",
                table: "Commentable",
                column: "Solution_TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Commentable_SubmittedByUserId",
                table: "Commentable",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Commentable_TeamId",
                table: "Commentable",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptainVotes_CandidateId",
                table: "CaptainVotes",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptainVotes_TeamId",
                table: "CaptainVotes",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_CaptainVotes_VoterId",
                table: "CaptainVotes",
                column: "VoterId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTeamSolutions_FileId",
                table: "FileTeamSolutions",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTeamSolutions_TeamSolutionId",
                table: "FileTeamSolutions",
                column: "TeamSolutionId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId",
                table: "TeamMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_AssignmentId",
                table: "Teams",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CourseId",
                table: "Teams",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatorId",
                table: "Teams",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_AspNetUsers_SubmittedByUserId",
                table: "Commentable",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_Commentable_Solution_TaskId",
                table: "Commentable",
                column: "Solution_TaskId",
                principalTable: "Commentable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable",
                column: "TaskId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_Teams_TeamId",
                table: "Commentable",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_AspNetUsers_SubmittedByUserId",
                table: "Commentable");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_Commentable_Solution_TaskId",
                table: "Commentable");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_Teams_TeamId",
                table: "Commentable");

            migrationBuilder.DropTable(
                name: "CaptainVotes");

            migrationBuilder.DropTable(
                name: "FileTeamSolutions");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Commentable_Solution_TaskId",
                table: "Commentable");

            migrationBuilder.DropIndex(
                name: "IX_Commentable_SubmittedByUserId",
                table: "Commentable");

            migrationBuilder.DropIndex(
                name: "IX_Commentable_TeamId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "AllowJoinTeam",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "AllowLeaveTeam",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "AllowStudentTransferCaptain",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "CaptainMode",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "CopyGroupsFromPreviousAssignment",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "FixedCaptainId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "MaxTeamSize",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "MinTeamSize",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "PredefinedTeamsCount",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Solution_Score",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Solution_Status",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Solution_TaskId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "SourceAssignmentId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_Deadline",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_MaxScore",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamAssignment_SolvableAfterDeadline",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TeamSolution_Text",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "VotingDurationHours",
                table: "Commentable");

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "Commentable",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable",
                column: "TaskId",
                principalTable: "Commentable",
                principalColumn: "Id");
        }
    }
}
