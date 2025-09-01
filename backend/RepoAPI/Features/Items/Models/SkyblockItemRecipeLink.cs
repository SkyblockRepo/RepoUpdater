using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Features.Recipes.Models;
using RepoAPI.Features.Wiki.Templates.ItemTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Items.Models;

/// <summary>
/// A simple link table between an Item and the recipes that produce it.
/// Used for name mismatches like DAEDALUS_AXE and DAEDALUS_BLADE (thanks Hypixel)
/// </summary>
public class SkyblockItemRecipeLink
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	
	[MaxLength(512)]
	public required string RecipeId { get; set; }

}

public class SkyblockItemRecipeLinkConfiguration : IEntityTypeConfiguration<SkyblockItemRecipeLink>
{
	public void Configure(EntityTypeBuilder<SkyblockItemRecipeLink> builder)
	{
		builder.HasKey(x => new { x.InternalId, x.RecipeId });
		builder.HasIndex(x => x.RecipeId);
	}
}