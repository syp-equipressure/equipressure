using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChatMessagesKeepDeletedShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Horse_HorseId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_MeasurementGroup_MeasurementGroupId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Horse_HorseId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "HorseId",
                principalSchema: "EquiPressure",
                principalTable: "Horse",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_MeasurementGroup_MeasurementGroupId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "MeasurementGroupId",
                principalSchema: "EquiPressure",
                principalTable: "MeasurementGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_Horse_HorseId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessage_MeasurementGroup_MeasurementGroupId",
                schema: "EquiPressure",
                table: "ChatMessage");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_Horse_HorseId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "HorseId",
                principalSchema: "EquiPressure",
                principalTable: "Horse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessage_MeasurementGroup_MeasurementGroupId",
                schema: "EquiPressure",
                table: "ChatMessage",
                column: "MeasurementGroupId",
                principalSchema: "EquiPressure",
                principalTable: "MeasurementGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
