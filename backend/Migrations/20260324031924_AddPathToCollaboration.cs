using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPathToCollaboration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "Collaborations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Path",
                table: "Collaborations");
        }
    }
}
