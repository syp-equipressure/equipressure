using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPushTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushToken",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UpdatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushToken_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushToken_PersonId",
                schema: "EquiPressure",
                table: "PushToken",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PushToken_Token",
                schema: "EquiPressure",
                table: "PushToken",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushToken",
                schema: "EquiPressure");
        }
    }
}
