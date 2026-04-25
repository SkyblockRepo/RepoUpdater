using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SkyblockRepo.Models;
using SkyblockRepo.Models.Neu;

namespace SkyblockRepo.StaticData;

internal enum RepoSectionParserStage
{
	PrimaryRepo = 0,
	NeuItems = 1,
	ExtendedMetadata = 2,
}

internal interface IRepoSectionParser
{
	string Name { get; }
	RepoSectionParserStage Stage { get; }
	Task ApplyAsync(RepoSectionLoadContext context, CancellationToken cancellationToken);
}

internal sealed class SkyblockRepoDataBuilder
{
	public SkyblockRepoDataBuilder()
	{
		Data = new SkyblockRepoData();
	}

	public SkyblockRepoData Data { get; }

	public SkyblockRepoData Build() => Data;
}

internal sealed class RepoSectionLoadContext(
	IRepoSnapshot mainSnapshot,
	IRepoSnapshot? neuSnapshot,
	HypixelCollectionsApiResponse? collectionsResponse,
	SkyblockRepoDataBuilder builder,
	JsonSerializerOptions serializerOptions,
	ILogger logger)
{
	public IRepoSnapshot MainSnapshot { get; } = mainSnapshot;
	public IRepoSnapshot? NeuSnapshot { get; } = neuSnapshot;
	public HypixelCollectionsApiResponse? CollectionsResponse { get; } = collectionsResponse;
	public SkyblockRepoDataBuilder Builder { get; } = builder;
	public SkyblockRepoData Data => Builder.Data;
	public JsonSerializerOptions SerializerOptions { get; } = serializerOptions;
	public ILogger Logger { get; } = logger;
	public bool HasNeuRepo => NeuSnapshot is not null;

	public async Task<T?> ReadNeuConstantAsync<T>(string fileName, CancellationToken cancellationToken = default) where T : class
	{
		if (NeuSnapshot is null)
		{
			return null;
		}

		return await ReadJsonAsync<T>(NeuSnapshot, $"constants/{fileName}", cancellationToken);
	}

	public async Task<T?> ReadMainFileAsync<T>(string relativePath, CancellationToken cancellationToken = default) where T : class
	{
		return await ReadJsonAsync<T>(MainSnapshot, relativePath, cancellationToken);
	}

	private async Task<T?> ReadJsonAsync<T>(IRepoSnapshot snapshot, string relativePath, CancellationToken cancellationToken) where T : class
	{
		if (!snapshot.FileExists(relativePath))
		{
			Logger.LogDebug("Static metadata source file not found: {Path}", $"{snapshot.SourcePath}::{relativePath}");
			return null;
		}

		await using var stream = await snapshot.OpenReadAsync(relativePath, cancellationToken);
		if (stream is null)
		{
			return null;
		}

		return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken);
	}
}

internal static partial class StaticMetadataParserUtils
{
	public static ReadOnlyCollection<T> ToReadOnlyCollection<T>(IEnumerable<T>? items)
	{
		return Array.AsReadOnly((items ?? []).ToArray());
	}

	public static ReadOnlyDictionary<string, T> ToReadOnlyDictionary<T>(IEnumerable<KeyValuePair<string, T>> items, IEqualityComparer<string>? comparer = null)
	{
		var dictionary = new Dictionary<string, T>(comparer ?? StringComparer.OrdinalIgnoreCase);
		foreach (var (key, value) in items)
		{
			dictionary[key] = value;
		}

		return new ReadOnlyDictionary<string, T>(dictionary);
	}

	public static ReadOnlyDictionary<int, T> ToReadOnlyDictionary<T>(IEnumerable<KeyValuePair<int, T>> items)
	{
		var dictionary = new Dictionary<int, T>();
		foreach (var (key, value) in items)
		{
			dictionary[key] = value;
		}

		return new ReadOnlyDictionary<int, T>(dictionary);
	}

	public static SkyblockDisplayIcon BuildTextureIcon(string? texture, string? skullOwner = null)
	{
		return new SkyblockDisplayIcon
		{
			Texture = texture,
			SkullOwner = skullOwner,
		};
	}

	public static SkyblockDisplayIcon BuildItemIcon(string? itemId)
	{
		return new SkyblockDisplayIcon
		{
			ItemId = itemId,
		};
	}

	public static SkyblockDisplayIcon BuildIconFromUrlOrItem(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return new SkyblockDisplayIcon();
		}

		if (value.StartsWith("/api/head/", StringComparison.OrdinalIgnoreCase))
		{
			return BuildTextureIcon(value["/api/head/".Length..]);
		}

		if (value.StartsWith("/api/item/", StringComparison.OrdinalIgnoreCase))
		{
			return BuildItemIcon(value["/api/item/".Length..].Replace(':', '-'));
		}

		return BuildItemIcon(value);
	}

	public static string CleanText(string? value, bool stripFormatting = true)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var cleaned = value.Replace("Â", string.Empty, StringComparison.Ordinal);
		if (stripFormatting)
		{
			cleaned = FormattingCodeRegex().Replace(cleaned, string.Empty);
		}

		return cleaned.Trim();
	}

	public static string TitleCaseId(string? id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			return string.Empty;
		}

		var textInfo = CultureInfo.InvariantCulture.TextInfo;
		return textInfo.ToTitleCase(id.Replace('_', ' ').ToLowerInvariant());
	}

	public static string NormalizeItemId(string value)
	{
		return value.Replace(':', '-').ToUpperInvariant();
	}

	public static NeuItemData? GetNeuItem(RepoSectionLoadContext context, string itemId)
	{
		if (context.Data.NeuItems.TryGetValue(itemId, out var exactMatch))
		{
			return exactMatch;
		}

		var normalizedId = NormalizeItemId(itemId);
		if (context.Data.NeuItems.TryGetValue(normalizedId, out var normalizedMatch))
		{
			return normalizedMatch;
		}

		return null;
	}

	public static string? GetItemName(RepoSectionLoadContext context, string itemId)
	{
		var itemName = context.Data.Items.GetValueOrDefault(itemId)?.Name;
		if (!string.IsNullOrWhiteSpace(itemName))
		{
			return itemName;
		}

		var neuName = GetNeuItem(context, itemId)?.DisplayName;
		return string.IsNullOrWhiteSpace(neuName) ? null : CleanText(neuName);
	}

	public static string? GetItemRarity(RepoSectionLoadContext context, string itemId)
	{
		var repoRarity = context.Data.Items.GetValueOrDefault(itemId)?.Data?.Tier;
		if (!string.IsNullOrWhiteSpace(repoRarity))
		{
			return repoRarity;
		}

		return ParseRarityFromLore(GetNeuItem(context, itemId)?.Lore);
	}

	public static SkyblockDisplayIcon GetItemIcon(RepoSectionLoadContext context, string itemId)
	{
		var neuItem = GetNeuItem(context, itemId);
		if (neuItem is not null)
		{
			if (string.Equals(neuItem.ItemId, "minecraft:skull", StringComparison.OrdinalIgnoreCase))
			{
				var texture = SkyblockRepoRegexUtils.ExtractSkullTexture(neuItem.NbtTag);
				if (texture is not null)
				{
					return BuildTextureIcon(texture.Value);
				}
			}
		}

		return BuildItemIcon(itemId);
	}

	public static string? ParseRarityFromLore(IEnumerable<string>? lore)
	{
		foreach (var line in (lore ?? []).Reverse())
		{
			var cleaned = CleanText(line);
			if (string.IsNullOrWhiteSpace(cleaned))
			{
				continue;
			}

			var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0)
			{
				continue;
			}

			var rarity = parts[0].ToUpperInvariant();
			if (KnownRarities.Contains(rarity))
			{
				return rarity;
			}
		}

		return null;
	}

	public static string? ExtractRarityLabel(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var tokens = CleanText(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return tokens.FirstOrDefault(token => KnownRarities.Contains(token.ToUpperInvariant()));
	}

	public static ReadOnlyCollection<int> ExtractThresholds(IEnumerable<int>? values)
	{
		return ToReadOnlyCollection(values ?? []);
	}

	private static readonly HashSet<string> KnownRarities =
	[
		"COMMON",
		"UNCOMMON",
		"RARE",
		"EPIC",
		"LEGENDARY",
		"MYTHIC",
		"DIVINE",
		"SPECIAL",
		"VERY",
	];

	[GeneratedRegex("§.", RegexOptions.Compiled)]
	private static partial Regex FormattingCodeRegex();
}
