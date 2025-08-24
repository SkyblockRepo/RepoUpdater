using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepoAPI.Features.Items.Models;

public class SkyblockItem
{
	[MaxLength(512)]
	public required string ItemId { get; set; }
    
	public double NpcSellPrice { get; set; }
    
	/// <summary>
	/// Hypixel item data from /resources/skyblock/items
	/// </summary>
	[Column(TypeName = "jsonb")]
	public ItemResponse? Data { get; set; }
}

public class SkyblockItemConfiguration : IEntityTypeConfiguration<SkyblockItem>
{
	public void Configure(EntityTypeBuilder<SkyblockItem> builder)
	{
		builder.HasKey(x => x.ItemId);
	}
}