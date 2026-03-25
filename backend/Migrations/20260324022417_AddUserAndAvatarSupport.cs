using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndAvatarSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agents_Name",
                table: "Agents");

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Agents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Agents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Name",
                table: "Agents",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_UserId",
                table: "Agents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.Sql(@"
                INSERT INTO ""Users"" (""Id"", ""Username"", ""Email"", ""PasswordHash"", ""Role"", ""CreatedAt"", ""UpdatedAt"")
                VALUES ('default-user-id', 'admin', 'admin@mafstudio.com', '$2a$11$default.hash.for.migration', 'admin', NOW(), NOW())
                ON CONFLICT DO NOTHING
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Agents"" SET ""UserId"" = 'default-user-id', ""Avatar"" = '🤖' WHERE ""UserId"" IS NULL OR ""UserId"" = ''
            ");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Agents",
                type: "text",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_Users_UserId",
                table: "Agents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_Users_UserId",
                table: "Agents");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Agents_Name",
                table: "Agents");

            migrationBuilder.DropIndex(
                name: "IX_Agents_UserId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Agents");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Name",
                table: "Agents",
                column: "Name",
                unique: true);
        }
    }
}
