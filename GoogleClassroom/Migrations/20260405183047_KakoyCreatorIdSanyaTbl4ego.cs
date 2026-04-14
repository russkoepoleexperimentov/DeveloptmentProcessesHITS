using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoogleClass.Migrations
{
    /// <inheritdoc />
    public partial class KakoyCreatorIdSanyaTbl4ego : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Teams_AspNetUsers_CreatorId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CreatorId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Teams");

            migrationBuilder.AddColumn<Guid>(
                name: "FixedCaptainId",
                table: "Teams",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FixedCaptainId",
                table: "Teams");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CreatorId",
                table: "Teams",
                column: "CreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_AspNetUsers_CreatorId",
                table: "Teams",
                column: "CreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
