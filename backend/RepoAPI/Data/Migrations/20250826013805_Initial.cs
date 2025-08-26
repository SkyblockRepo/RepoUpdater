using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;

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
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    NpcValue = table.Column<double>(type: "double precision", nullable: false),
                    Flags_Tradable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Bazaarable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Auctionable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Reforgeable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Enchantable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Museumable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Soulboundable = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Data = table.Column<ItemResponse>(type: "jsonb", nullable: true),
                    RawTemplate = table.Column<string>(type: "text", nullable: true),
                    TemplateData = table.Column<ItemTemplateDto>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockItems", x => x.InternalId);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockPets",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RawTemplate = table.Column<string>(type: "text", nullable: true),
                    TemplateData = table.Column<ItemTemplateDto>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockPets", x => x.InternalId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkyblockItems");

            migrationBuilder.DropTable(
                name: "SkyblockPets");
        }
    }
}
