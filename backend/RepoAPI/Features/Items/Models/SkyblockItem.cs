using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RepoAPI.Features.Items.Models;

public class SkyblockItem
{
	[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int InternalId { get; set; }
	[MaxLength(256)]
	public required string Id { get; set; }
}

public class SkyblockItemEntityConfiguration : IEntityTypeConfiguration<SkyblockItem>
{
	public void Configure(EntityTypeBuilder<SkyblockItem> builder)
	{
		builder.HasIndex(e => e.Id); // Not a unique index just in case Hypixel messes up
	}
}