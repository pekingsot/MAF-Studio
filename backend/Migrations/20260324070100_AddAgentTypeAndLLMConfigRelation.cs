using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTypeAndLLMConfigRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitAccessToken",
                table: "Collaborations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitBranch",
                table: "Collaborations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitEmail",
                table: "Collaborations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitRepositoryUrl",
                table: "Collaborations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitUsername",
                table: "Collaborations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LLMConfigId",
                table: "Agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DefaultSystemPrompt = table.Column<string>(type: "text", nullable: true),
                    DefaultTemperature = table.Column<double>(type: "double precision", nullable: false),
                    DefaultMaxTokens = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_LLMConfigId",
                table: "Agents",
                column: "LLMConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTypes_Code",
                table: "AgentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_LLMConfigs_LLMConfigId",
                table: "Agents",
                column: "LLMConfigId",
                principalTable: "LLMConfigs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_LLMConfigs_LLMConfigId",
                table: "Agents");

            migrationBuilder.DropTable(
                name: "AgentTypes");

            migrationBuilder.DropIndex(
                name: "IX_Agents_LLMConfigId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "GitAccessToken",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "GitBranch",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "GitEmail",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "GitRepositoryUrl",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "GitUsername",
                table: "Collaborations");

            migrationBuilder.DropColumn(
                name: "LLMConfigId",
                table: "Agents");
        }
    }
}
