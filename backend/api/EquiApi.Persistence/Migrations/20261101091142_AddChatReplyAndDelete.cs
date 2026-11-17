using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatReplyAndDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "DeletedAt",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "ReplyToId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_ChatMessage_ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "ReplyToId",
                principalSchema: "EquiPressure",
                principalTable: "ChatMessage",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_ChatMessage_ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessage_ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "ReplyToId",
                schema: "EquiPressure",
                table: "ChatMessage");
        }
    }
}
