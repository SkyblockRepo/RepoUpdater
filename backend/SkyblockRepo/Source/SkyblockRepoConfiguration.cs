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
	/// GitHub repository URL for SkyblockRepo. Defaults to https://skyblockrepo.com/repo
	/// Can be changed to use a fork or a different repository.
	/// Note: The repository must follow the same structure as the original SkyblockRepo repository.
	/// </summary>
	public string SkyblockRepoUrl { get; set; } = "https://skyblockrepo.com/repo";
	
	/// <summary>
	/// Path to the zip file of the main branch of the SkyblockRepo repository. Defaults to /archive/refs/heads/main.zip
	/// </summary>
	public string SkyblockRepoZipPath { get; set; } = "/archive/refs/heads/main.zip";
	
	/// <summary>
	/// The endpoint to poll for updates with ETag support. Defaults to https://api.github.com/repos/SkyblockRepo/Repo/branches/main
	/// </summary>
	public string SkyblockRepoApiEndpoint { get; set; } = "https://api.github.com/repos/SkyblockRepo/Repo/branches/main";

	/// <summary>
	/// Set this to a local path to use a local clone of the repo instead of cloning from GitHub.
	/// </summary>
	public string? LocalRepoPath { get; set; }
}