using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepoAPI.Features.Items.Models;
using RepoAPI.Util;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Recipes.Models;

public class SkyblockRecipe
{
	[Key]
	[MapperIgnore]
	public Guid Id { get; set; } = Guid.CreateVersion7();
	
	[MaxLength(512)]
	[MapperIgnore]
	public string? Name { get; set; }
	
	public RecipeType Type { get; set; } = RecipeType.Crafting;
	
	[MaxLength(512)]
	public string? ResultInternalId { get; set; }
	
	public int ResultQuantity { get; set; } = 1;
	
	public List<RecipeIngredient> Ingredients { get; set; } = [];
	
	[MapperIgnore]
	public required string Hash { get; set; }
}

public class RecipeIngredient
{
	[MapperIgnore]
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	[MaxLength(512)]
	[MapperIgnore]
	public required Guid RecipeId { get; set; }
	
	[MaxLength(64)]
	public string? Slot { get; set; }
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	public int Quantity { get; set; } = 1;
}

[JsonStringEnum]
public enum RecipeType
{
	Crafting,
}

public class SkyblockRecipeConfiguration : IEntityTypeConfiguration<SkyblockRecipe>
{
	public void Configure(EntityTypeBuilder<SkyblockRecipe> builder)
	{
		builder.HasIndex(r => r.Hash).IsUnique();
		builder.HasIndex(r => r.ResultInternalId);
		builder.HasIndex(r => r.Type);
		
		builder.HasOne<SkyblockItem>()
			.WithMany(i => i.Recipes)
			.HasForeignKey(r => r.ResultInternalId);
		
		builder.HasMany(r => r.Ingredients)
			.WithOne()
			.HasForeignKey(i => i.RecipeId)
			.OnDelete(DeleteBehavior.Cascade);
		
		builder.Navigation(r => r.Ingredients).AutoInclude();
	}
}