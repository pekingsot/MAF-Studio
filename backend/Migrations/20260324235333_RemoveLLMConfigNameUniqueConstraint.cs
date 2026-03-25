using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLLMConfigNameUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LLMConfigs_Name",
                table: "LLMConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_LLMConfigs_Name",
                table: "LLMConfigs",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LLMConfigs_Name",
                table: "LLMConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_LLMConfigs_Name",
                table: "LLMConfigs",
                column: "Name",
                unique: true);
        }
    }
}
