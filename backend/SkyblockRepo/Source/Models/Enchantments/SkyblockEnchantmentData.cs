using System.ComponentModel.DataAnnotations;

namespace SkyblockRepo.Models;

public class SkyblockEnchantmentData
{
	[MaxLength(512)]
	public string InternalId { get; set; } = string.Empty;
	
	[MaxLength(512)]
	public string? Name { get; set; }
	
	[MaxLength(64)]
	public string Source { get; set; } = "HypixelWiki";
	
	public int MinLevel { get; set; } = 1;
	public int MaxLevel { get; set; } = 1;
	
	public List<string> Items { get; set; } = [];
}
