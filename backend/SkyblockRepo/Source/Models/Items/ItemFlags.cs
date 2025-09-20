using System.Text.Json.Serialization;

namespace SkyblockRepo.Models;

public record struct ItemFlags()
{
	public bool Tradable { get; set; }
	public bool Bazaarable { get; set; }
	public bool Auctionable { get; set; }
	public bool Reforgeable { get; set; }
	public bool Enchantable { get; set; }
	public bool Museumable { get; set; }
	public bool Soulboundable { get; set; }
	public bool Sackable { get; set; }
	
	[JsonExtensionData]
	public SortedDictionary<string, object> Other { get; set; } = new();
}