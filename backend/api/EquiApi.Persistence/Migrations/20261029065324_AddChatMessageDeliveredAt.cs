using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageDeliveredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "DeliveredAt",
                schema: "EquiPressure",
                table: "ChatMessage",
                type: "timestamp with time zone",
                nullable: true);

            // read messages were delivered as well
            migrationBuilder.Sql(
                """UPDATE "EquiPressure"."ChatMessage" SET "DeliveredAt" = "ReadAt" WHERE "ReadAt" IS NOT NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                schema: "EquiPressure",
                table: "ChatMessage");
        }
    }
}
