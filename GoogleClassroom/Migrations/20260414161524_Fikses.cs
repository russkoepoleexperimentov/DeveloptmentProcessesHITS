using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class Fikses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileSolution_Commentable_SolutionId",
                table: "FileSolution");

            migrationBuilder.DropForeignKey(
                name: "FK_FileSolution_UserFiles_FileId",
                table: "FileSolution");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileSolution",
                table: "FileSolution");

            migrationBuilder.RenameTable(
                name: "FileSolution",
                newName: "FileSolutions");

            migrationBuilder.RenameIndex(
                name: "IX_FileSolution_SolutionId",
                table: "FileSolutions",
                newName: "IX_FileSolutions_SolutionId");

            migrationBuilder.RenameIndex(
                name: "IX_FileSolution_FileId",
                table: "FileSolutions",
                newName: "IX_FileSolutions_FileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileSolutions",
                table: "FileSolutions",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileSolutions_Commentable_SolutionId",
                table: "FileSolutions",
                column: "SolutionId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileSolutions_UserFiles_FileId",
                table: "FileSolutions",
                column: "FileId",
                principalTable: "UserFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileSolutions_Commentable_SolutionId",
                table: "FileSolutions");

            migrationBuilder.DropForeignKey(
                name: "FK_FileSolutions_UserFiles_FileId",
                table: "FileSolutions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FileSolutions",
                table: "FileSolutions");

            migrationBuilder.RenameTable(
                name: "FileSolutions",
                newName: "FileSolution");

            migrationBuilder.RenameIndex(
                name: "IX_FileSolutions_SolutionId",
                table: "FileSolution",
                newName: "IX_FileSolution_SolutionId");

            migrationBuilder.RenameIndex(
                name: "IX_FileSolutions_FileId",
                table: "FileSolution",
                newName: "IX_FileSolution_FileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FileSolution",
                table: "FileSolution",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileSolution_Commentable_SolutionId",
                table: "FileSolution",
                column: "SolutionId",
                principalTable: "Commentable",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileSolution_UserFiles_FileId",
                table: "FileSolution",
                column: "FileId",
                principalTable: "UserFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
