using System.IO.Compression;

namespace SkyblockRepo;

internal interface IRepoSnapshot : IAsyncDisposable
{
	string SourcePath { get; }
	bool FileExists(string relativePath);
	ValueTask<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default);
	IReadOnlyList<RepoSnapshotFile> GetFiles(string relativeDirectory, string searchPattern);
}

internal readonly record struct RepoSnapshotFile(string RelativePath, string NameWithoutExtension);

internal static class RepoSnapshot
{
	public static async Task<IRepoSnapshot> OpenAsync(
		string repoPath,
		RepoStorageMode storageMode,
		string zipFileName,
		CancellationToken cancellationToken = default)
	{
		return storageMode switch
		{
			RepoStorageMode.ExtractedDirectory => new DirectoryRepoSnapshot(repoPath),
			RepoStorageMode.ZipArchive => await ZipRepoSnapshot.CreateAsync(Path.Combine(repoPath, zipFileName), cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(storageMode), storageMode, "Unsupported repo storage mode.")
		};
	}

	public static bool Exists(string repoPath, RepoStorageMode storageMode, string zipFileName)
	{
		return storageMode switch
		{
			RepoStorageMode.ExtractedDirectory => Directory.Exists(repoPath),
			RepoStorageMode.ZipArchive => File.Exists(Path.Combine(repoPath, zipFileName)),
			_ => false
		};
	}

	public static string NormalizeRelativePath(string relativePath)
	{
		return relativePath.Replace('\\', '/').Trim('/');
	}

	public static string NormalizeArchiveEntryPath(string fullName, string commonRoot)
	{
		var normalized = NormalizeRelativePath(fullName);
		if (string.IsNullOrEmpty(commonRoot))
		{
			return normalized;
		}

		var rootPrefix = $"{commonRoot}/";
		return normalized.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
			? normalized[rootPrefix.Length..]
			: normalized;
	}

	public static string GetCommonArchiveRoot(IEnumerable<string> fullNames)
	{
		var normalized = fullNames
			.Select(NormalizeRelativePath)
			.Where(path => !string.IsNullOrWhiteSpace(path))
			.ToArray();

		if (normalized.Length == 0)
		{
			return string.Empty;
		}

		var firstSegments = normalized
			.Select(path => path.Split('/', 2)[0])
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();

		if (firstSegments.Length != 1)
		{
			return string.Empty;
		}

		var root = firstSegments[0];
		var rootPrefix = $"{root}/";
		return normalized.All(path => path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
			? root
			: string.Empty;
	}

	public static string GetParentDirectory(string relativePath)
	{
		var normalized = NormalizeRelativePath(relativePath);
		var separatorIndex = normalized.LastIndexOf('/');
		return separatorIndex < 0 ? string.Empty : normalized[..separatorIndex];
	}

	public static string GetFileNameWithoutExtension(string relativePath)
	{
		return Path.GetFileNameWithoutExtension(relativePath.Replace('/', Path.DirectorySeparatorChar));
	}

	public static string GetFullPath(string rootPath, string relativePath)
	{
		var normalized = NormalizeRelativePath(relativePath);
		if (string.IsNullOrEmpty(normalized))
		{
			return rootPath;
		}

		var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return Path.Combine(segments.Prepend(rootPath).ToArray());
	}
}

internal sealed class DirectoryRepoSnapshot(string repoPath) : IRepoSnapshot
{
	public string SourcePath { get; } = repoPath;

	public bool FileExists(string relativePath)
	{
		return File.Exists(RepoSnapshot.GetFullPath(SourcePath, relativePath));
	}

	public ValueTask<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
	{
		var fullPath = RepoSnapshot.GetFullPath(SourcePath, relativePath);
		Stream? stream = File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
		return ValueTask.FromResult(stream);
	}

	public IReadOnlyList<RepoSnapshotFile> GetFiles(string relativeDirectory, string searchPattern)
	{
		var directoryPath = RepoSnapshot.GetFullPath(SourcePath, relativeDirectory);
		if (!Directory.Exists(directoryPath))
		{
			return [];
		}

		return Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly)
			.Where(path => !path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
			.Select(path => new RepoSnapshotFile(
				RepoSnapshot.NormalizeRelativePath(Path.GetRelativePath(SourcePath, path)),
				Path.GetFileNameWithoutExtension(path)))
			.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class ZipRepoSnapshot : IRepoSnapshot
{
	private readonly Dictionary<string, byte[]> _files;

	private ZipRepoSnapshot(string zipPath, Dictionary<string, byte[]> files)
	{
		SourcePath = zipPath;
		_files = files;
	}

	public string SourcePath { get; }

	public static async Task<ZipRepoSnapshot> CreateAsync(string zipPath, CancellationToken cancellationToken = default)
	{
		await using var stream = File.OpenRead(zipPath);
		using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

		var entries = archive.Entries
			.Where(entry => !string.IsNullOrEmpty(entry.Name))
			.ToArray();
		var commonRoot = RepoSnapshot.GetCommonArchiveRoot(entries.Select(entry => entry.FullName));

		var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		foreach (var entry in entries)
		{
			var relativePath = RepoSnapshot.NormalizeArchiveEntryPath(entry.FullName, commonRoot);
			if (string.IsNullOrWhiteSpace(relativePath))
			{
				continue;
			}

			await using var entryStream = entry.Open();
			await using var memoryStream = new MemoryStream();
			await entryStream.CopyToAsync(memoryStream, cancellationToken);
			files[relativePath] = memoryStream.ToArray();
		}

		return new ZipRepoSnapshot(zipPath, files);
	}

	public bool FileExists(string relativePath)
	{
		return _files.ContainsKey(RepoSnapshot.NormalizeRelativePath(relativePath));
	}

	public ValueTask<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
	{
		var normalizedPath = RepoSnapshot.NormalizeRelativePath(relativePath);
		Stream? stream = _files.TryGetValue(normalizedPath, out var bytes)
			? new MemoryStream(bytes, writable: false)
			: null;
		return ValueTask.FromResult(stream);
	}

	public IReadOnlyList<RepoSnapshotFile> GetFiles(string relativeDirectory, string searchPattern)
	{
		var normalizedDirectory = RepoSnapshot.NormalizeRelativePath(relativeDirectory);

		return _files.Keys
			.Where(path => IsDirectChild(path, normalizedDirectory) && MatchesPattern(path, searchPattern))
			.Select(path => new RepoSnapshotFile(path, RepoSnapshot.GetFileNameWithoutExtension(path)))
			.OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	private static bool IsDirectChild(string relativePath, string relativeDirectory)
	{
		return string.Equals(
			RepoSnapshot.GetParentDirectory(relativePath),
			relativeDirectory,
			StringComparison.OrdinalIgnoreCase);
	}

	private static bool MatchesPattern(string relativePath, string searchPattern)
	{
		if (string.Equals(searchPattern, "*.json", StringComparison.OrdinalIgnoreCase))
		{
			return relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
		}

		return string.Equals(Path.GetFileName(relativePath), searchPattern, StringComparison.OrdinalIgnoreCase);
	}
}
