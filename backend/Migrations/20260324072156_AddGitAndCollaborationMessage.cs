using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MAFStudio.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddGitAndCollaborationMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CollaborationId",
                table: "AgentMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentMessages_CollaborationId",
                table: "AgentMessages",
                column: "CollaborationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentMessages_Collaborations_CollaborationId",
                table: "AgentMessages",
                column: "CollaborationId",
                principalTable: "Collaborations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentMessages_Collaborations_CollaborationId",
                table: "AgentMessages");

            migrationBuilder.DropIndex(
                name: "IX_AgentMessages_CollaborationId",
                table: "AgentMessages");

            migrationBuilder.DropColumn(
                name: "CollaborationId",
                table: "AgentMessages");
        }
    }
}
