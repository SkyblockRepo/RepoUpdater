using System.Collections.Concurrent;

namespace SkyblockRepo;

public interface ISkyblockRepoMatcher
{
	/// <summary>
	/// The type of item this matcher can handle.
	/// </summary>
	Type ItemType { get; }

	/// <summary>
	/// Get string value of an attribute from your item type.
	/// </summary>
	string? GetAttributeString(object item, string attribute);

	/// <summary>
	/// Get string value of skyblock ID from your item type.
	/// </summary>
	string? GetSkyblockId(object item);

	/// <summary>
	/// Get string value of item name from your item type.
	/// </summary>
	string? GetName(object item);
}

/// <summary>
/// Base class that converts strongly typed matcher implementations into the non-generic interface expected by the library.
/// </summary>
public abstract class SkyblockRepoMatcher<TItem> : ISkyblockRepoMatcher where TItem : class
{
	Type ISkyblockRepoMatcher.ItemType => typeof(TItem);

	string? ISkyblockRepoMatcher.GetAttributeString(object item, string attribute) =>
		GetAttributeString(Cast(item), attribute);

	string? ISkyblockRepoMatcher.GetSkyblockId(object item) =>
		GetSkyblockId(Cast(item));

	string? ISkyblockRepoMatcher.GetName(object item) =>
		GetName(Cast(item));

	/// <summary>
	/// Get string value of an attribute from your item type.
	/// </summary>
	protected virtual string? GetAttributeString(TItem item, string attribute) => null;

	/// <summary>
	/// Get string value of skyblock ID from your item type.
	/// </summary>
	protected virtual string? GetSkyblockId(TItem item) => null;

	/// <summary>
	/// Get string value of item name from your item type.
	/// </summary>
	protected virtual string? GetName(TItem item) => null;

	private static TItem Cast(object item)
	{
		if (item is TItem typed)
		{
			return typed;
		}

		throw new ArgumentException($"Item must be of type {typeof(TItem).FullName}", nameof(item));
	}
}

/// <summary>
/// Registry for matchers keyed by their supported item type.
/// </summary>
public class SkyblockRepoMatcherRegistry
{
	private readonly ConcurrentDictionary<Type, ISkyblockRepoMatcher> _matchers = new();

	/// <summary>
	/// Register a matcher implementation for its supported item type.
	/// </summary>
	public void Register(ISkyblockRepoMatcher matcher)
	{
		if (matcher is null)
		{
			throw new ArgumentNullException(nameof(matcher));
		}

		_matchers[matcher.ItemType] = matcher;
	}

	/// <summary>
	/// Try to resolve a matcher for the provided item type.
	/// </summary>
	public bool TryGetMatcher(Type itemType, out ISkyblockRepoMatcher? matcher)
	{
		if (itemType is null)
		{
			throw new ArgumentNullException(nameof(itemType));
		}

		if (_matchers.TryGetValue(itemType, out matcher))
		{
			return true;
		}

		foreach (var pair in _matchers)
		{
			if (pair.Key.IsAssignableFrom(itemType))
			{
				matcher = pair.Value;
				return true;
			}
		}

		matcher = null;
		return false;
	}

	/// <summary>
	/// Try to resolve a matcher for the provided item instance.
	/// </summary>
	public bool TryGetMatcher(object? item, out ISkyblockRepoMatcher? matcher)
	{
		if (item is null)
		{
			matcher = null;
			return false;
		}

		return TryGetMatcher(item.GetType(), out matcher);
	}

	public IEnumerable<ISkyblockRepoMatcher> GetRegisteredMatchers() => _matchers.Values;
}