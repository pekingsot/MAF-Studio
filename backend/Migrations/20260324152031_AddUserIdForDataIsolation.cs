using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdForDataIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RagDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "LLMConfigs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Collaborations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AgentTypes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RagDocuments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AgentTypes");
        }
    }
}
