namespace SkyblockRepo.Models;

/// <summary>
/// Result of matching a consumer item against the Skyblock repository data.
/// </summary>
public sealed class SkyblockItemMatch
{
	public SkyblockItemMatch(SkyblockItemData item, SkyblockItemVariant? variant)
	{
		Item = item ?? throw new ArgumentNullException(nameof(item));
		Variant = variant;
	}

	/// <summary>
	/// The item found in the repo.
	/// </summary>
	public SkyblockItemData Item { get; }

	/// <summary>
	/// The variant definition that matched the consumer item, when available.
	/// </summary>
	public SkyblockItemVariant? Variant { get; }

	/// <summary>
	/// Convenience accessor for the variant's item data.
	/// </summary>
	public SkyblockItemVariantData? VariantData => Variant?.Item;
}
