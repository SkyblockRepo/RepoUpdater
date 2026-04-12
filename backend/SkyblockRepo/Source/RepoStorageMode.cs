namespace SkyblockRepo;

/// <summary>
/// Controls how a repository is stored on disk and loaded into memory.
/// </summary>
public enum RepoStorageMode
{
	/// <summary>
	/// Repository contents are extracted to a directory tree on disk.
	/// </summary>
	ExtractedDirectory,

	/// <summary>
	/// Repository contents are stored as a single zip archive and inflated in memory during reload.
	/// </summary>
	ZipArchive
}
