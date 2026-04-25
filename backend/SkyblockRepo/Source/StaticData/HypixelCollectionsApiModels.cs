using System.Text.Json.Serialization;

namespace SkyblockRepo.StaticData;

internal sealed class HypixelCollectionsApiResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; init; }

	[JsonPropertyName("lastUpdated")]
	public long LastUpdated { get; init; }

	[JsonPropertyName("version")]
	public string? Version { get; init; }

	[JsonPropertyName("collections")]
	public Dictionary<string, HypixelCollectionsApiCategory>? Collections { get; init; }

	public DateTimeOffset? GetLastUpdatedAt()
	{
		return LastUpdated > 0
			? DateTimeOffset.FromUnixTimeMilliseconds(LastUpdated)
			: null;
	}
}

internal sealed class HypixelCollectionsApiCategory
{
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	[JsonPropertyName("items")]
	public Dictionary<string, HypixelCollectionsApiItem>? Items { get; init; }
}

internal sealed class HypixelCollectionsApiItem
{
	[JsonPropertyName("name")]
	public string Name { get; init; } = string.Empty;

	[JsonPropertyName("maxTiers")]
	public int MaxTiers { get; init; }

	[JsonPropertyName("tiers")]
	public List<HypixelCollectionsApiTier>? Tiers { get; init; }
}

internal sealed class HypixelCollectionsApiTier
{
	[JsonPropertyName("tier")]
	public int Tier { get; init; }

	[JsonPropertyName("amountRequired")]
	public int AmountRequired { get; init; }
}
