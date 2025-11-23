using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Core.Models;
using RepoAPI.Features.Items.Models;
using RepoAPI.Util;
using Riok.Mapperly.Abstractions;
using SkyblockRepo.Models;

namespace RepoAPI.Features.Recipes.Models;

public class SkyblockRecipe : IVersionedEntity
{
	#region IVersionedEntity Implementation
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	[MapperIgnore]
	public int Id { get; set; }
	
	[MapperIgnore]
	public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
	
	[MapperIgnore]
	public bool Latest { get; set; } = true;
	
	/// <summary>
	/// This is called "InternalId" for the versioned entity, but is just the hash of the recipe data.
	/// This is used to determine if a recipe already exists in the database.
	/// </summary>
	[MaxLength(512), MapperIgnore]
	public string InternalId { get; set; }
	#endregion
	
	[MaxLength(512)]
	public string? Name { get; set; }
	
	public RecipeType Type { get; set; } = RecipeType.Crafting;
	
	[MaxLength(512)]
	public string? ResultInternalId { get; set; }
	
	public int ResultQuantity { get; set; } = 1;
	
	public List<RecipeIngredient> Ingredients { get; set; } = [];

	/// <summary>
	/// Hash of the recipe data, used to determine if a recipe already exists in the database.
	/// </summary>
	[NotMapped, MapperIgnore]
	public string Hash
	{
		get => InternalId;
		set => InternalId = value;
	}
}

public class RecipeIngredient
{
	[MapperIgnore]
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MapperIgnore]
	public int RecipeId { get; set; }
	
	[MaxLength(64)]
	public string? Slot { get; set; }
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public int Quantity { get; set; } = 1;
}

public class SkyblockRecipeConfiguration : IEntityTypeConfiguration<SkyblockRecipe>
{
	public void Configure(EntityTypeBuilder<SkyblockRecipe> builder)
	{
		builder.HasKey(x => x.Id);
		
		builder.HasIndex(x => x.Name);
		builder.HasIndex(x => new { ResultInternalId = x.ResultInternalId, x.Latest });
		
		builder.HasIndex(x => x.IngestedAt);
		
		builder.HasQueryFilter(x => x.Latest);
		
		builder.HasIndex(r => r.Hash).IsUnique();
		builder.HasIndex(r => r.ResultInternalId);
		builder.HasIndex(r => r.Type);
		
		builder.HasMany(r => r.Ingredients)
			.WithOne()
			.HasForeignKey(i => i.RecipeId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.Navigation(r => r.Ingredients).AutoInclude();
	}
}