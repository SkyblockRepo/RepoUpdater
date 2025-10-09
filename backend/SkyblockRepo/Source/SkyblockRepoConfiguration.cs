namespace SkyblockRepo;

public class SkyblockRepoConfiguration
{
	/// <summary>
	/// The root path where all repo files will be stored.
	/// </summary>
	public string FileStoragePath { get; set; } =
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkyblockRepo");

	/// <summary>
	/// Set to true to also download and use data from the NotEnoughUpdates repository.
	/// </summary>
	public bool UseNeuRepo { get; set; } = false;

	/// <summary>
	/// Settings for the primary SkyblockRepo.
	/// </summary>
	public RepoSettings SkyblockRepo { get; set; } = new()
	{
		Name = "skyblockrepo",
		Url = "https://github.com/SkyblockRepo/Repo",
		ZipPath = "/archive/refs/heads/main.zip",
		ApiEndpoint = "https://api.github.com/repos/SkyblockRepo/Repo/commits/main"
	};

	/// <summary>
	/// Settings for the NotEnoughUpdates repository.
	/// </summary>
	public RepoSettings NeuRepo { get; set; } = new()
	{
		Name = "neu",
		Url = "https://github.com/NotEnoughUpdates/NotEnoughUpdates-REPO",
		ZipPath = "/archive/refs/heads/master.zip",
		ApiEndpoint = "https://api.github.com/repos/NotEnoughUpdates/NotEnoughUpdates-REPO/commits/master"
	};
	
	/// <summary>
	/// Configuration for how SkyblockRepo should check for variants from your item type.
	/// </summary>
	private SkyblockRepoMatcherRegistry _matcher = new();

	/// <summary>
	/// Registry of matchers used to resolve consumer item types to repo data.
	/// </summary>
	public SkyblockRepoMatcherRegistry Matcher
	{
		get => _matcher;
		set => _matcher = value ?? throw new ArgumentNullException(nameof(value));
	}
}

/// <summary>
/// Contains all the settings required to update a single repository.
/// </summary>
public class RepoSettings
{
	/// <summary>
	/// The unique name for this repository (e.g., "skyblockrepo", "neu").
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The base URL of the repository (e.g., https://github.com/User/Repo).
	/// </summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>
	/// The relative path to the zip archive from the base URL.
	/// </summary>
	public string ZipPath { get; set; } = string.Empty;

	/// <summary>
	/// The API endpoint to poll for updates using an ETag.
	/// </summary>
	public string ApiEndpoint { get; set; } = string.Empty;

	/// <summary>
	/// An optional local file path to use instead of downloading.
	/// </summary>
	public string? LocalPath { get; set; }
}