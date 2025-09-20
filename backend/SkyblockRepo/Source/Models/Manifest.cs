namespace SkyblockRepo.Models;

public class Manifest
{
	public int Version { get; set; }
	public ManifestPaths Paths { get; set; } = new();
}

public class ManifestPaths
{
	public string? Items { get; set; }
	public string? Pets { get; set; }
	public string? Enchantments { get; set; }
	public string? Npcs { get; set; }
	public string? Zones { get; set; }
}