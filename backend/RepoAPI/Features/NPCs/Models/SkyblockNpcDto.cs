using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RepoAPI.Features.NPCs.NpcTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.NPCs.Models;

[Mapper]
public static partial class SkyblockNpcMapper
{
	[UserMapping(Default = true)]
	public static SkyblockNpcDto ToDto(this SkyblockNpc pet)
	{
		var data = pet.TemplateData;
		return new SkyblockNpcDto
		{
			InternalId = pet.InternalId,
			Name = pet.Name,
			Flags = data?.Flags ?? new NpcFlags(),
			Location = data?.Location ?? new NpcLocation(),
			Visitor = data?.Visitor
		};
	}
}

public class SkyblockNpcDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Name { get; set; }
	public NpcFlags Flags { get; set; } = new();
	
	public NpcLocation Location { get; set; } = new();
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public NpcGardenVisitor? Visitor { get; set; }
}