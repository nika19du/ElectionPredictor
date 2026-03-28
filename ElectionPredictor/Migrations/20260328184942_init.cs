using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ElectionPredictor.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Elections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ElectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Elections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectionResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ElectionId = table.Column<int>(type: "integer", nullable: false),
                    PartyId = table.Column<int>(type: "integer", nullable: false),
                    VotePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Seats = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectionResults_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ElectionResults_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PollEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ElectionId = table.Column<int>(type: "integer", nullable: false),
                    PartyId = table.Column<int>(type: "integer", nullable: false),
                    Pollster = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PollDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExternalKey = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PollEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PollEntries_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PollEntries_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectionResults_ElectionId",
                table: "ElectionResults",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectionResults_PartyId",
                table: "ElectionResults",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PollEntries_ElectionId",
                table: "PollEntries",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PollEntries_ExternalKey",
                table: "PollEntries",
                column: "ExternalKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PollEntries_PartyId",
                table: "PollEntries",
                column: "PartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectionResults");

            migrationBuilder.DropTable(
                name: "PollEntries");

            migrationBuilder.DropTable(
                name: "Elections");

            migrationBuilder.DropTable(
                name: "Parties");
        }
    }
}
