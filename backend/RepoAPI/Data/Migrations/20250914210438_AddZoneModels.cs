using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepoAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkyblockZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latest = table.Column<bool>(type: "boolean", nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockZones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockZones_IngestedAt",
                table: "SkyblockZones",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockZones_InternalId",
                table: "SkyblockZones",
                column: "InternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockZones_InternalId_Latest",
                table: "SkyblockZones",
                columns: new[] { "InternalId", "Latest" });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockZones_Name",
                table: "SkyblockZones",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkyblockZones");
        }
    }
}
