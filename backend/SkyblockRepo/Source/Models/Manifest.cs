namespace SkyblockRepo.Models;

public class Manifest
{
	public int Version { get; set; }
	public ManifestPaths Paths { get; set; } = new();
}

public class ManifestPaths
{
	public string Items { get; set; } = "items";
	public string Pets { get; set; } = "pets";
	public string Enchantments { get; set; } = "enchantments";
	public string Npcs { get; set; } = "npcs";
	public string Zones { get; set; } = "zones";
	public string Misc { get; set; } = "misc";
}