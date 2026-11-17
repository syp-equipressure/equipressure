using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHorseAndPersonImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                schema: "EquiPressure",
                table: "Person",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "ImageUpdatedAt",
                schema: "EquiPressure",
                table: "Person",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                schema: "EquiPressure",
                table: "Horse",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "ImageUpdatedAt",
                schema: "EquiPressure",
                table: "Horse",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HorseImage",
                schema: "EquiPressure",
                columns: table => new
                {
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorseImage", x => x.HorseId);
                    table.ForeignKey(
                        name: "FK_HorseImage_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonImage",
                schema: "EquiPressure",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonImage", x => x.PersonId);
                    table.ForeignKey(
                        name: "FK_PersonImage_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HorseImage",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "PersonImage",
                schema: "EquiPressure");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "ImageUpdatedAt",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.DropColumn(
                name: "ImageUpdatedAt",
                schema: "EquiPressure",
                table: "Horse");
        }
    }
}
