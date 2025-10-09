using SkyblockRepo.Models;

namespace SkyblockRepo;

public static class SkyblockItemMatchingExtensions
{
	/// <summary>
	/// Attempts to find the variant that matches the provided source item using the supplied matcher.
	/// Returns <c>null</c> when the item has no variants or none match.
	/// </summary>
	public static SkyblockItemVariant? GetMatchingVariant(this SkyblockItemData item, object sourceItem)
	{
		var matcher = SkyblockRepoClient.Instance.GetMatcher(item);
		return item.GetMatchingVariant(sourceItem, matcher);
	}
	
	/// <summary>
	/// Attempts to find the variant that matches the provided source item using the supplied matcher.
	/// Returns <c>null</c> when the item has no variants or none match.
	/// </summary>
	public static SkyblockItemVariant? GetMatchingVariant(this SkyblockItemData item, object sourceItem, ISkyblockRepoMatcher matcher)
	{
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(sourceItem);
        ArgumentNullException.ThrowIfNull(matcher);

		if (item.Variants is null || item.Variants.Count == 0)
		{
			return null;
		}

		foreach (var variant in item.Variants)
		{
			if (variant?.By is null)
			{
				continue;
			}

			var comparisonValue = GetComparisonValue(sourceItem, matcher, variant.By);
			if (comparisonValue is null)
			{
				continue;
			}

			if (MatchesDefinition(comparisonValue, variant.By))
			{
				return variant;
			}
		}

		return null;
	}

	private static string? GetComparisonValue(object sourceItem, ISkyblockRepoMatcher matcher, ItemVariationDefinition definition) => definition.Type switch
	{
		VariationType.Name => matcher.GetName(sourceItem),
		VariationType.Attribute => string.IsNullOrWhiteSpace(definition.Key)
			? null
			: matcher.GetAttributeString(sourceItem, definition.Key),
		_ => null
	};

	private static bool MatchesDefinition(string? candidate, ItemVariationDefinition definition)
	{
		if (string.IsNullOrWhiteSpace(candidate))
		{
			return false;
		}

		var value = candidate.Trim();
		const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
		var hasConstraint = false;

		if (!string.IsNullOrWhiteSpace(definition.Exact))
		{
			hasConstraint = true;
			if (!string.Equals(value, definition.Exact, comparison))
			{
				return false;
			}
		}

		if (!string.IsNullOrWhiteSpace(definition.StartsWith))
		{
			hasConstraint = true;
			if (!value.StartsWith(definition.StartsWith, comparison))
			{
				return false;
			}
		}

		if (!string.IsNullOrWhiteSpace(definition.EndsWith))
		{
			hasConstraint = true;
			if (!value.EndsWith(definition.EndsWith, comparison))
			{
				return false;
			}
		}

		if (!string.IsNullOrWhiteSpace(definition.Contains))
		{
			hasConstraint = true;
			if (value.IndexOf(definition.Contains, comparison) < 0)
			{
				return false;
			}
		}

		return hasConstraint || !string.IsNullOrWhiteSpace(value);
	}
}
