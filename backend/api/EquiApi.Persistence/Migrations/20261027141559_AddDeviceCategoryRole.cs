using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceCategoryRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                schema: "EquiPressure",
                table: "DeviceCategory",
                type: "text",
                nullable: false,
                defaultValue: "Equestrian");

            // the professional version is the one for saddlers
            migrationBuilder.Sql(
                """UPDATE "EquiPressure"."DeviceCategory" SET "Role" = 'Saddler' WHERE "Name" = 'Professional';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "EquiPressure",
                table: "DeviceCategory");
        }
    }
}
