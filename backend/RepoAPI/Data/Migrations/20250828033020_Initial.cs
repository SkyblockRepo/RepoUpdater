using System;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RepoAPI.Features.Wiki.Templates.PetTemplate;

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
                name: "SkyblockEnchantments",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MinLevel = table.Column<int>(type: "integer", nullable: false),
                    MaxLevel = table.Column<int>(type: "integer", nullable: false),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockEnchantments", x => x.InternalId);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockItems",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NpcValue = table.Column<double>(type: "double precision", nullable: false),
                    Flags_Tradable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Bazaarable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Auctionable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Reforgeable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Enchantable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Museumable = table.Column<bool>(type: "boolean", nullable: false),
                    Flags_Soulboundable = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Lore = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<ItemResponse>(type: "jsonb", nullable: true),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
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
                    TemplateData = table.Column<PetTemplateDto>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockPets", x => x.InternalId);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockItemRecipeLinks",
                columns: table => new
                {
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    RecipeId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockItemRecipeLinks", x => new { x.InternalId, x.RecipeId });
                    table.ForeignKey(
                        name: "FK_SkyblockItemRecipeLinks_SkyblockItems_InternalId",
                        column: x => x.InternalId,
                        principalTable: "SkyblockItems",
                        principalColumn: "InternalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockRecipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ResultInternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ResultQuantity = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkyblockRecipes_SkyblockItems_ResultInternalId",
                        column: x => x.ResultInternalId,
                        principalTable: "SkyblockItems",
                        principalColumn: "InternalId");
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<Guid>(type: "uuid", maxLength: 512, nullable: false),
                    Slot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_SkyblockRecipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "SkyblockRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockItemRecipeLinks_RecipeId",
                table: "SkyblockItemRecipeLinks",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_Hash",
                table: "SkyblockRecipes",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_ResultInternalId",
                table: "SkyblockRecipes",
                column: "ResultInternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_Type",
                table: "SkyblockRecipes",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "SkyblockEnchantments");

            migrationBuilder.DropTable(
                name: "SkyblockItemRecipeLinks");

            migrationBuilder.DropTable(
                name: "SkyblockPets");

            migrationBuilder.DropTable(
                name: "SkyblockRecipes");

            migrationBuilder.DropTable(
                name: "SkyblockItems");
        }
    }
}
