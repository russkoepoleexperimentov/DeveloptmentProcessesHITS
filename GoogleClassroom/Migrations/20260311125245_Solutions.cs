using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class Solutions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comment_AspNetUsers_UserId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Comment_ParentCommentId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Commentable_CommentableId",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_FilePost_Commentable_PostId",
                table: "FilePost");

            migrationBuilder.DropForeignKey(
                name: "FK_FilePost_UserFiles_FileId",
                table: "FilePost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FilePost",
                table: "FilePost");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comment",
                table: "Comment");

            migrationBuilder.RenameTable(
                name: "FilePost",
                newName: "FilePosts");

            migrationBuilder.RenameTable(
                name: "Comment",
                newName: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_FilePost_PostId",
                table: "FilePosts",
                newName: "IX_FilePosts_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_FilePost_FileId",
                table: "FilePosts",
                newName: "IX_FilePosts_FileId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_UserId",
                table: "Comments",
                newName: "IX_Comments_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_ParentCommentId",
                table: "Comments",
                newName: "IX_Comments_ParentCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_CommentableId",
                table: "Comments",
                newName: "IX_Comments_CommentableId");

            migrationBuilder.AddColumn<long>(
                name: "Score",
                table: "Commentable",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Solution_Text",
                table: "Commentable",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Commentable",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Commentable",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_FilePosts",
                table: "FilePosts",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "FileSolution",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSolution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileSolution_Commentable_SolutionId",
                        column: x => x.SolutionId,
                        principalTable: "Commentable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileSolution_UserFiles_FileId",
                        column: x => x.FileId,
                        principalTable: "UserFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Commentable_TaskId",
                table: "Commentable",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Commentable_UserId",
                table: "Commentable",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSolution_FileId",
                table: "FileSolution",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileSolution_SolutionId",
                table: "FileSolution",
                column: "SolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_AspNetUsers_UserId",
                table: "Commentable",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable",
                column: "TaskId",
                principalTable: "Commentable",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Commentable_CommentableId",
                table: "Comments",
                column: "CommentableId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Comments_ParentCommentId",
                table: "Comments",
                column: "ParentCommentId",
                principalTable: "Comments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FilePosts_Commentable_PostId",
                table: "FilePosts",
                column: "PostId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilePosts_UserFiles_FileId",
                table: "FilePosts",
                column: "FileId",
                principalTable: "UserFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_AspNetUsers_UserId",
                table: "Commentable");

            migrationBuilder.DropForeignKey(
                name: "FK_Commentable_Commentable_TaskId",
                table: "Commentable");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_AspNetUsers_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Commentable_CommentableId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Comments_ParentCommentId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_FilePosts_Commentable_PostId",
                table: "FilePosts");

            migrationBuilder.DropForeignKey(
                name: "FK_FilePosts_UserFiles_FileId",
                table: "FilePosts");

            migrationBuilder.DropTable(
                name: "FileSolution");

            migrationBuilder.DropIndex(
                name: "IX_Commentable_TaskId",
                table: "Commentable");

            migrationBuilder.DropIndex(
                name: "IX_Commentable_UserId",
                table: "Commentable");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FilePosts",
                table: "FilePosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Solution_Text",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Commentable");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Commentable");

            migrationBuilder.RenameTable(
                name: "FilePosts",
                newName: "FilePost");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Comment");

            migrationBuilder.RenameIndex(
                name: "IX_FilePosts_PostId",
                table: "FilePost",
                newName: "IX_FilePost_PostId");

            migrationBuilder.RenameIndex(
                name: "IX_FilePosts_FileId",
                table: "FilePost",
                newName: "IX_FilePost_FileId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_UserId",
                table: "Comment",
                newName: "IX_Comment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_ParentCommentId",
                table: "Comment",
                newName: "IX_Comment_ParentCommentId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_CommentableId",
                table: "Comment",
                newName: "IX_Comment_CommentableId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FilePost",
                table: "FilePost",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comment",
                table: "Comment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_AspNetUsers_UserId",
                table: "Comment",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Comment_ParentCommentId",
                table: "Comment",
                column: "ParentCommentId",
                principalTable: "Comment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Commentable_CommentableId",
                table: "Comment",
                column: "CommentableId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilePost_Commentable_PostId",
                table: "FilePost",
                column: "PostId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FilePost_UserFiles_FileId",
                table: "FilePost",
                column: "FileId",
                principalTable: "UserFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
