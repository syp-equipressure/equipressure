using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StoreRoleNamesAsText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "EquiPressure",
                table: "AccountRole",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Equestrian");

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Saddler");

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Guest");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Name",
                schema: "EquiPressure",
                table: "AccountRole",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: 10);

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: 20);

            migrationBuilder.UpdateData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: 30);
        }
    }
}
