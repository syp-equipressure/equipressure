using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessage",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    AcceptedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    SenderId = table.Column<int>(type: "integer", nullable: false),
                    RecipientId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: true),
                    MeasurementGroupId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessage_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessage_MeasurementGroup_MeasurementGroupId",
                        column: x => x.MeasurementGroupId,
                        principalSchema: "EquiPressure",
                        principalTable: "MeasurementGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessage_Person_RecipientId",
                        column: x => x.RecipientId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessage_Person_SenderId",
                        column: x => x.SenderId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_HorseId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_MeasurementGroupId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "MeasurementGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_RecipientId_ReadAt",
                schema: "EquiPressure",
                table: "ChatMessage",
                columns: new[] { "RecipientId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessage_SenderId_RecipientId_SentAt",
                schema: "EquiPressure",
                table: "ChatMessage",
                columns: new[] { "SenderId", "RecipientId", "SentAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessage",
                schema: "EquiPressure");
        }
    }
}
