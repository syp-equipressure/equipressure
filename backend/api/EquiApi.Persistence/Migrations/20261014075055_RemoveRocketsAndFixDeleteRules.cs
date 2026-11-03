using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRocketsAndFixDeleteRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horse_Address_AddressId",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.DropForeignKey(
                name: "FK_Person_Address_AddressId",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropTable(
                name: "Rocket",
                schema: "EquiPressure");

            migrationBuilder.InsertData(
                schema: "EquiPressure",
                table: "AccountRole",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, 10 },
                    { 2, 20 },
                    { 3, 30 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRole_Name",
                schema: "EquiPressure",
                table: "AccountRole",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Horse_Address_AddressId",
                schema: "EquiPressure",
                table: "Horse",
                column: "AddressId",
                principalSchema: "EquiPressure",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Person_Address_AddressId",
                schema: "EquiPressure",
                table: "Person",
                column: "AddressId",
                principalSchema: "EquiPressure",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Horse_Address_AddressId",
                schema: "EquiPressure",
                table: "Horse");

            migrationBuilder.DropForeignKey(
                name: "FK_Person_Address_AddressId",
                schema: "EquiPressure",
                table: "Person");

            migrationBuilder.DropIndex(
                name: "IX_AccountRole_Name",
                schema: "EquiPressure",
                table: "AccountRole");

            migrationBuilder.DeleteData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "EquiPressure",
                table: "AccountRole",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.CreateTable(
                name: "Rocket",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Manufacturer = table.Column<string>(type: "text", nullable: false),
                    MaxThrust = table.Column<double>(type: "double precision", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    PayloadDeltaV = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rocket", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Horse_Address_AddressId",
                schema: "EquiPressure",
                table: "Horse",
                column: "AddressId",
                principalSchema: "EquiPressure",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Person_Address_AddressId",
                schema: "EquiPressure",
                table: "Person",
                column: "AddressId",
                principalSchema: "EquiPressure",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
