using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueConstraintMeasurement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Measurement_GroupId_Pace_Hand",
                schema: "EquiPressure",
                table: "Measurement",
                columns: new[] { "GroupId", "Pace", "Hand" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Measurement_GroupId_Pace_Hand",
                schema: "EquiPressure",
                table: "Measurement");
        }
    }
}
