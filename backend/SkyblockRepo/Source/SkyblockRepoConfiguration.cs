namespace SkyblockRepo;

public class SkyblockRepoConfiguration
{
	/// <summary>
	/// The path where repo files will be stored to. Defaults to an OS-appropriate folder
	/// Ex: %LOCALAPPDATA%/SkyblockRepo on Windows (adapts for other OSes)
	/// </summary>
	public string FileStoragePath { get; set; } = 
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SkyblockRepo");
	
	/// <summary>
	/// Polling interval for checking for updates to the repo data. Defaults to 1 hour.
	/// </summary>
	public TimeSpan PollingInterval { get; set; } = TimeSpan.FromHours(1);
	
	/// <summary>
	/// Whether to enable automatic polling for updates. If false, updates must be triggered manually. Defaults to true.
	/// </summary>
	public bool EnableAutoPolling { get; set; } = true;
	
	/// <summary>
	/// GitHub repository URL for SkyblockRepo. Defaults to https://skyblockrepo.com/repo
	/// Can be changed to use a fork or a different repository.
	/// Note: The repository must follow the same structure as the original SkyblockRepo repository.
	/// </summary>
	public string SkyblockRepoUrl { get; set; } = "https://skyblockrepo.com/repo";

	/// <summary>
	/// Set this to a local path to use a local clone of the repo instead of cloning from GitHub.
	/// </summary>
	public string? LocalRepoPath { get; set; }
}