namespace SkyblockRepo.Models;

public class DownloadMeta
{
	public int Version { get; set; }
	public DateTimeOffset LastUpdated { get; set; }
	public string? ETag { get; set; }
}