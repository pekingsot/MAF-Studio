using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMConfigAndRagTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OperationLogs_Users_UserId",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "OperationLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "OperationLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "OperationLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<long>(
                name: "Duration",
                table: "OperationLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "OperationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuccess",
                table: "OperationLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequestData",
                table: "OperationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseData",
                table: "OperationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "OperationLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LLMConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Temperature = table.Column<double>(type: "double precision", nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    ContextWindow = table.Column<int>(type: "integer", nullable: false),
                    TopP = table.Column<double>(type: "double precision", nullable: true),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: true),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: true),
                    StopSequences = table.Column<string>(type: "text", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LLMConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RagDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SplitMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ChunkSize = table.Column<int>(type: "integer", nullable: true),
                    ChunkOverlap = table.Column<int>(type: "integer", nullable: true),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Group = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RagDocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    VectorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagDocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RagDocumentChunks_RagDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "RagDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LLMConfigs_Name",
                table: "LLMConfigs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LLMConfigs_Provider",
                table: "LLMConfigs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_RagDocumentChunks_DocumentId",
                table: "RagDocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RagDocuments_FileName",
                table: "RagDocuments",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_RagDocuments_Status",
                table: "RagDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SystemConfigs_Key",
                table: "SystemConfigs",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LLMConfigs");

            migrationBuilder.DropTable(
                name: "RagDocumentChunks");

            migrationBuilder.DropTable(
                name: "SystemConfigs");

            migrationBuilder.DropTable(
                name: "RagDocuments");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "IsSuccess",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "RequestData",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "ResponseData",
                table: "OperationLogs");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "OperationLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "OperationLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "OperationLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "OperationLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "OperationLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OperationLogs_Users_UserId",
                table: "OperationLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
