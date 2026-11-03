using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileContentType",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatFile",
                schema: "EquiPressure",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatFile", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_ChatFile_ChatMessage_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "EquiPressure",
                        principalTable: "ChatMessage",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatFile",
                schema: "EquiPressure");

            migrationBuilder.DropColumn(
                name: "FileContentType",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "FileName",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropColumn(
                name: "FileSize",
                schema: "EquiPressure",
                table: "ChatMessage");
        }
    }
}
