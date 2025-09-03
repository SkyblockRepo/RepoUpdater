using System.Text.Json.Serialization;

namespace RepoAPI.Features.Pets.PetTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock Pet template.
/// </summary>
public class PetTemplateDto
{
	public string? InternalId { get; set; }
	
	[JsonExtensionData]
	public SortedDictionary<string, object> AdditionalProperties { get; set; } = new();
}