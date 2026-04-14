using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssoziationHorseBreed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horse_Breed_BreedId",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.DropIndex(
                name: "IX_Horse_BreedId",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.DropColumn(
                name: "BreedId",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.CreateTable(
                name: "HorseBreed",
                schema: "EquiPressure",
                columns: table => new
                {
                    BreedId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorseBreed", x => new { x.HorseId, x.BreedId });
                    table.ForeignKey(
                        name: "FK_HorseBreed_Breed_BreedId",
                        column: x => x.BreedId,
                        principalSchema: "EquiPressure",
                        principalTable: "Breed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HorseBreed_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HorseBreed_BreedId",
                schema: "EquiPressure",
                table: "HorseBreed",
                column: "BreedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorseBreed",
                schema: "EquiPressure");

            migrationBuilder.AddColumn<int>(
                name: "BreedId",
                schema: "EquiPressure",
                table: "Horse",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Horse_BreedId",
                schema: "EquiPressure",
                table: "Horse",
                column: "BreedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Horse_Breed_BreedId",
                schema: "EquiPressure",
                table: "Horse",
                column: "BreedId",
                principalSchema: "EquiPressure",
                principalTable: "Breed",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
