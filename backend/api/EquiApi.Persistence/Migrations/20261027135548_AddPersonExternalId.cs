using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                schema: "EquiPressure",
                table: "Person",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_ExternalId",
                schema: "EquiPressure",
                table: "Person",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Person_ExternalId",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                schema: "EquiPressure",
                table: "Person");
        }
    }
}
