using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepoAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkyblockItems",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    NpcSellPrice = table.Column<double>(type: "double precision", nullable: false),
                    Data = table.Column<ItemResponse>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockItems", x => x.ItemId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkyblockItems");
        }
    }
}
