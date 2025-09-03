using System;
using System.Text.Json;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using RepoAPI.Features.Items.Models;

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
                name: "DataIngestionBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataIngestionBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockEnchantments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latest = table.Column<bool>(type: "boolean", nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MinLevel = table.Column<int>(type: "integer", nullable: false),
                    MaxLevel = table.Column<int>(type: "integer", nullable: false),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockEnchantments", x => x.Id);
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
                });

            migrationBuilder.CreateTable(
                name: "SkyblockItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latest = table.Column<bool>(type: "boolean", nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    NpcValue = table.Column<double>(type: "double precision", nullable: false),
                    Flags = table.Column<ItemFlags>(type: "jsonb", nullable: false),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Lore = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<ItemResponse>(type: "jsonb", nullable: true),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockPets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latest = table.Column<bool>(type: "boolean", nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Lore = table.Column<string>(type: "text", nullable: false),
                    RawTemplate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockPets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingDeprecations",
                columns: table => new
                {
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    EntityIdToDeprecate = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingDeprecations", x => new { x.BatchId, x.EntityIdToDeprecate, x.EntityType });
                    table.ForeignKey(
                        name: "FK_PendingDeprecations_DataIngestionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "DataIngestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingEntityChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EntityData = table.Column<JsonDocument>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingEntityChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingEntityChanges_DataIngestionBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "DataIngestionBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockRecipes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Latest = table.Column<bool>(type: "boolean", nullable: false),
                    InternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ResultInternalId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ResultQuantity = table.Column<int>(type: "integer", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    SkyblockItemId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockRecipes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkyblockRecipes_SkyblockItems_SkyblockItemId",
                        column: x => x.SkyblockItemId,
                        principalTable: "SkyblockItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecipeId = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_DataIngestionBatches_Status",
                table: "DataIngestionBatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PendingEntityChanges_BatchId",
                table: "PendingEntityChanges",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_RecipeId",
                table: "RecipeIngredients",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockEnchantments_IngestedAt",
                table: "SkyblockEnchantments",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockEnchantments_InternalId",
                table: "SkyblockEnchantments",
                column: "InternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockEnchantments_InternalId_Latest",
                table: "SkyblockEnchantments",
                columns: new[] { "InternalId", "Latest" });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockEnchantments_Name",
                table: "SkyblockEnchantments",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockItemRecipeLinks_RecipeId",
                table: "SkyblockItemRecipeLinks",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockItems_IngestedAt",
                table: "SkyblockItems",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockItems_InternalId",
                table: "SkyblockItems",
                column: "InternalId",
                unique: true,
                filter: "\"Latest\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockItems_Name",
                table: "SkyblockItems",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockPets_IngestedAt",
                table: "SkyblockPets",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockPets_InternalId",
                table: "SkyblockPets",
                column: "InternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockPets_InternalId_Latest",
                table: "SkyblockPets",
                columns: new[] { "InternalId", "Latest" });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockPets_Name",
                table: "SkyblockPets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_Hash",
                table: "SkyblockRecipes",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_IngestedAt",
                table: "SkyblockRecipes",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_Name",
                table: "SkyblockRecipes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_ResultInternalId",
                table: "SkyblockRecipes",
                column: "ResultInternalId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_ResultInternalId_Latest",
                table: "SkyblockRecipes",
                columns: new[] { "ResultInternalId", "Latest" });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_SkyblockItemId",
                table: "SkyblockRecipes",
                column: "SkyblockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockRecipes_Type",
                table: "SkyblockRecipes",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingDeprecations");

            migrationBuilder.DropTable(
                name: "PendingEntityChanges");

            migrationBuilder.DropTable(
                name: "RecipeIngredients");

            migrationBuilder.DropTable(
                name: "SkyblockEnchantments");

            migrationBuilder.DropTable(
                name: "SkyblockItemRecipeLinks");

            migrationBuilder.DropTable(
                name: "SkyblockPets");

            migrationBuilder.DropTable(
                name: "DataIngestionBatches");

            migrationBuilder.DropTable(
                name: "SkyblockRecipes");

            migrationBuilder.DropTable(
                name: "SkyblockItems");
        }
    }
}
