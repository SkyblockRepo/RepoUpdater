using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RepoAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNPCShops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkyblockShops",
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
                    table.PrimaryKey("PK_SkyblockShops", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockShops_IngestedAt",
                table: "SkyblockShops",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockShops_InternalId",
                table: "SkyblockShops",
                column: "InternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockShops_InternalId_Latest",
                table: "SkyblockShops",
                columns: new[] { "InternalId", "Latest" });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockShops_Name",
                table: "SkyblockShops",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkyblockShops");
        }
    }
}
