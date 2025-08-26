using System.Text.Json.Serialization;

namespace RepoAPI.Features.Wiki.Templates.PetTemplate;

/// <summary>
/// Represents the parsed data from a Hypixel SkyBlock Pet template.
/// </summary>
public class PetTemplateDto
{
	public string? InternalId { get; set; }
	
	[JsonExtensionData]
	public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}