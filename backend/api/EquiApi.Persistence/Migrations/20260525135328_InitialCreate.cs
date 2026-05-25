using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EquiApi.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "EquiPressure");

            migrationBuilder.CreateTable(
                name: "AccountRole",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Breed",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Breed", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "City",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PLZ = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_City", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceCategory",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumOfAllowedPeople = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rocket",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: false),
                    MaxThrust = table.Column<double>(type: "double precision", nullable: false),
                    PayloadDeltaV = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rocket", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaddleCategory",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaddleCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Address",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Street = table.Column<string>(type: "text", nullable: true),
                    HouseNumber = table.Column<int>(type: "integer", nullable: true),
                    CityId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Address_City_CityId",
                        column: x => x.CityId,
                        principalSchema: "EquiPressure",
                        principalTable: "City",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Horse",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DateOfBirth = table.Column<LocalDate>(type: "date", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    AddressId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Horse_Address_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "EquiPressure",
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Person",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric", nullable: false),
                    DateOfBirth = table.Column<LocalDate>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    WebsiteLink = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AddressId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Person", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Person_Address_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "EquiPressure",
                        principalTable: "Address",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateTable(
                name: "Saddle",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Saddle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Saddle_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Saddle_SaddleCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "EquiPressure",
                        principalTable: "SaddleCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementDevice",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementDevice_DeviceCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "EquiPressure",
                        principalTable: "DeviceCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementDevice_Person_OwnerId",
                        column: x => x.OwnerId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonHorse",
                schema: "EquiPressure",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    IsOwner = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonHorse", x => new { x.PersonId, x.HorseId });
                    table.ForeignKey(
                        name: "FK_PersonHorse_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonHorse_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonRelationship",
                schema: "EquiPressure",
                columns: table => new
                {
                    EquestrianId = table.Column<int>(type: "integer", nullable: false),
                    SaddlerId = table.Column<int>(type: "integer", nullable: false),
                    IsContact = table.Column<bool>(type: "boolean", nullable: false),
                    IsFavourite = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRelationship", x => new { x.EquestrianId, x.SaddlerId });
                    table.ForeignKey(
                        name: "FK_PersonRelationship_Person_EquestrianId",
                        column: x => x.EquestrianId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonRelationship_Person_SaddlerId",
                        column: x => x.SaddlerId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonRoleAssignment",
                schema: "EquiPressure",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRoleAssignment", x => new { x.PersonId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_PersonRoleAssignment_AccountRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "EquiPressure",
                        principalTable: "AccountRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonRoleAssignment_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Release",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReleaseTimestamp = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    PersonId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Release", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Release_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceUser",
                schema: "EquiPressure",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceUser", x => new { x.DeviceId, x.UserId });
                    table.ForeignKey(
                        name: "FK_DeviceUser_MeasurementDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "EquiPressure",
                        principalTable: "MeasurementDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceUser_Person_UserId",
                        column: x => x.UserId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementGroup",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<LocalDate>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PersonId = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    SaddleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementGroup_Horse_HorseId",
                        column: x => x.HorseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Horse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementGroup_MeasurementDevice_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "EquiPressure",
                        principalTable: "MeasurementDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementGroup_Person_PersonId",
                        column: x => x.PersonId,
                        principalSchema: "EquiPressure",
                        principalTable: "Person",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementGroup_Saddle_SaddleId",
                        column: x => x.SaddleId,
                        principalSchema: "EquiPressure",
                        principalTable: "Saddle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Measurement",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Pace = table.Column<string>(type: "text", nullable: false),
                    Hand = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    GroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurement_MeasurementGroup_GroupId",
                        column: x => x.GroupId,
                        principalSchema: "EquiPressure",
                        principalTable: "MeasurementGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementEligibility",
                schema: "EquiPressure",
                columns: table => new
                {
                    ReleaseId = table.Column<int>(type: "integer", nullable: false),
                    MeasurementGroupId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementEligibility", x => new { x.MeasurementGroupId, x.ReleaseId });
                    table.ForeignKey(
                        name: "FK_MeasurementEligibility_MeasurementGroup_MeasurementGroupId",
                        column: x => x.MeasurementGroupId,
                        principalSchema: "EquiPressure",
                        principalTable: "MeasurementGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeasurementEligibility_Release_ReleaseId",
                        column: x => x.ReleaseId,
                        principalSchema: "EquiPressure",
                        principalTable: "Release",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementData",
                schema: "EquiPressure",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Data = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    MeasurementId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasurementData_Measurement_MeasurementId",
                        column: x => x.MeasurementId,
                        principalSchema: "EquiPressure",
                        principalTable: "Measurement",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Address_CityId",
                schema: "EquiPressure",
                table: "Address",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_City_PLZ_Name",
                schema: "EquiPressure",
                table: "City",
                columns: new[] { "PLZ", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceUser_UserId",
                schema: "EquiPressure",
                table: "DeviceUser",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Horse_AddressId",
                schema: "EquiPressure",
                table: "Horse",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Horse_Name",
                schema: "EquiPressure",
                table: "Horse",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_HorseBreed_BreedId",
                schema: "EquiPressure",
                table: "HorseBreed",
                column: "BreedId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurement_GroupId",
                schema: "EquiPressure",
                table: "Measurement",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Measurement_GroupId_Pace_Hand",
                schema: "EquiPressure",
                table: "Measurement",
                columns: new[] { "GroupId", "Pace", "Hand" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementData_MeasurementId",
                schema: "EquiPressure",
                table: "MeasurementData",
                column: "MeasurementId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementDevice_CategoryId",
                schema: "EquiPressure",
                table: "MeasurementDevice",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementDevice_OwnerId",
                schema: "EquiPressure",
                table: "MeasurementDevice",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementEligibility_ReleaseId",
                schema: "EquiPressure",
                table: "MeasurementEligibility",
                column: "ReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementGroup_DeviceId",
                schema: "EquiPressure",
                table: "MeasurementGroup",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementGroup_HorseId",
                schema: "EquiPressure",
                table: "MeasurementGroup",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementGroup_PersonId",
                schema: "EquiPressure",
                table: "MeasurementGroup",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementGroup_SaddleId",
                schema: "EquiPressure",
                table: "MeasurementGroup",
                column: "SaddleId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_AddressId",
                schema: "EquiPressure",
                table: "Person",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Person_DateOfBirth",
                schema: "EquiPressure",
                table: "Person",
                column: "DateOfBirth");

            migrationBuilder.CreateIndex(
                name: "IX_Person_Email",
                schema: "EquiPressure",
                table: "Person",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_FirstName_LastName",
                schema: "EquiPressure",
                table: "Person",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonHorse_HorseId",
                schema: "EquiPressure",
                table: "PersonHorse",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRelationship_SaddlerId",
                schema: "EquiPressure",
                table: "PersonRelationship",
                column: "SaddlerId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRoleAssignment_RoleId",
                schema: "EquiPressure",
                table: "PersonRoleAssignment",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Release_PersonId",
                schema: "EquiPressure",
                table: "Release",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_Saddle_CategoryId",
                schema: "EquiPressure",
                table: "Saddle",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Saddle_HorseId",
                schema: "EquiPressure",
                table: "Saddle",
                column: "HorseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceUser",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "HorseBreed",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "MeasurementData",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "MeasurementEligibility",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "PersonHorse",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "PersonRelationship",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "PersonRoleAssignment",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Rocket",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Breed",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Measurement",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Release",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "AccountRole",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "MeasurementGroup",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "MeasurementDevice",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Saddle",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "DeviceCategory",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Person",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Horse",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "SaddleCategory",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "Address",
                schema: "EquiPressure");

            migrationBuilder.DropTable(
                name: "City",
                schema: "EquiPressure");
        }
    }
}
