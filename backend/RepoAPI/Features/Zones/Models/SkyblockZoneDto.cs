using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using RepoAPI.Features.Wiki.Templates;
using RepoAPI.Features.Zones.NpcTemplate;
using Riok.Mapperly.Abstractions;

namespace RepoAPI.Features.Zones.Models;

[Mapper]
public static partial class SkyblockZoneMapper
{
	[UserMapping(Default = true)]
	public static SkyblockZoneDto ToDto(this SkyblockZone pet)
	{
		var data = pet.TemplateData;
		return new SkyblockZoneDto
		{
			InternalId = pet.InternalId,
			Name = pet.Name,
			Source = pet.Source,
			DiscoveryText = data?.DiscoveryText,
			Npcs = data?.Npcs,
			Mobs = data?.Mobs,
			MobDrops = data?.MobDrops,
			FairySouls = data?.FairySouls,
		};
	}
}

public class SkyblockZoneDto
{
	[MaxLength(512)]
	public required string InternalId { get; set; }
	public string? Name { get; set; }
	public string Source { get; set; } = "HypixelWiki";
	public string? DiscoveryText { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? Npcs { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? Mobs { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ListItem>? MobDrops { get; set; }
	
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<FairySoul>? FairySouls { get; set; }
}