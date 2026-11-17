using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRiderCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<LocalDate>(
                name: "DateOfBirth",
                schema: "EquiPressure",
                table: "Person",
                type: "date",
                nullable: true,
                oldClrType: typeof(LocalDate),
                oldType: "date");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                schema: "EquiPressure",
                table: "Person",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_CreatedById",
                schema: "EquiPressure",
                table: "Person",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Person_Person_CreatedById",
                schema: "EquiPressure",
                table: "Person",
                column: "CreatedById",
                principalSchema: "EquiPressure",
                principalTable: "Person",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Person_Person_CreatedById",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_Person_CreatedById",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.AlterColumn<LocalDate>(
                name: "DateOfBirth",
                schema: "EquiPressure",
                table: "Person",
                type: "date",
                nullable: false,
                defaultValue: new NodaTime.LocalDate(1, 1, 1),
                oldClrType: typeof(LocalDate),
                oldType: "date",
                oldNullable: true);
        }
    }
}
