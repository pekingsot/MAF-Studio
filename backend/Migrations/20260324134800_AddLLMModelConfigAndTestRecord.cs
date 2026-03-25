using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMModelConfigAndTestRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextWindow",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "FrequencyPenalty",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "MaxTokens",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "PresencePenalty",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "StopSequences",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "LLMConfigs");

            migrationBuilder.DropColumn(
                name: "TopP",
                table: "LLMConfigs");

            migrationBuilder.AddColumn<Guid>(
                name: "LLMModelConfigId",
                table: "Agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LLMModelConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LLMConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    ContextWindow = table.Column<int>(type: "integer", nullable: false),
                    TopP = table.Column<double>(type: "double precision", nullable: true),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: true),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: true),
                    StopSequences = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMModelConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLMModelConfigs_LLMConfigs_LLMConfigId",
                        column: x => x.LLMConfigId,
                        principalTable: "LLMConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LLMTestRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LLMConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    LLMModelConfigId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    TestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMTestRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LLMTestRecords_LLMConfigs_LLMConfigId",
                        column: x => x.LLMConfigId,
                        principalTable: "LLMConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LLMTestRecords_LLMModelConfigs_LLMModelConfigId",
                        column: x => x.LLMModelConfigId,
                        principalTable: "LLMModelConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_LLMModelConfigId",
                table: "Agents",
                column: "LLMModelConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMModelConfigs_LLMConfigId",
                table: "LLMModelConfigs",
                column: "LLMConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMModelConfigs_ModelName",
                table: "LLMModelConfigs",
                column: "ModelName");

            migrationBuilder.CreateIndex(
                name: "IX_LLMTestRecords_LLMConfigId",
                table: "LLMTestRecords",
                column: "LLMConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMTestRecords_LLMModelConfigId",
                table: "LLMTestRecords",
                column: "LLMModelConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_LLMTestRecords_TestedAt",
                table: "LLMTestRecords",
                column: "TestedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Agents_LLMModelConfigs_LLMModelConfigId",
                table: "Agents",
                column: "LLMModelConfigId",
                principalTable: "LLMModelConfigs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agents_LLMModelConfigs_LLMModelConfigId",
                table: "Agents");

            migrationBuilder.DropTable(
                name: "LLMTestRecords");

            migrationBuilder.DropTable(
                name: "LLMModelConfigs");

            migrationBuilder.DropIndex(
                name: "IX_Agents_LLMModelConfigId",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "LLMModelConfigId",
                table: "Agents");

            migrationBuilder.AddColumn<int>(
                name: "ContextWindow",
                table: "LLMConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FrequencyPenalty",
                table: "LLMConfigs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTokens",
                table: "LLMConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "LLMConfigs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PresencePenalty",
                table: "LLMConfigs",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StopSequences",
                table: "LLMConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "LLMConfigs",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TopP",
                table: "LLMConfigs",
                type: "double precision",
                nullable: true);
        }
    }
}
