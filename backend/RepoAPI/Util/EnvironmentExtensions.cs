namespace RepoAPI.Util;

public static class EnvironmentExtensions
{
	/// <summary>
	/// Checks if the current hosting environment name is "Testing".
	/// </summary>
	/// <param name="hostEnvironment">An instance of IHostEnvironment.</param>
	/// <returns>True if the environment name is "Testing", otherwise false.</returns>
	public static bool IsTesting(this IHostEnvironment hostEnvironment)
	{
		return hostEnvironment.IsEnvironment("Testing");
	}
}