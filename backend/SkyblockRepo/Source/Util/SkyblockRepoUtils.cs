namespace SkyblockRepo;

public static class SkyblockRepoUtils
{
	public static string GetSolutionPath()
	{
		var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
		var iterations = 0;
		const int maxIterations = 10;
	
		// Search upwards from the bin folder until we find the solution file (.sln)
		while (currentDir != null && currentDir.GetFiles("*.sln").Length == 0)
		{
			currentDir = currentDir.Parent;
			iterations++;
			if (iterations >= maxIterations) throw new InvalidOperationException("Could not find solution root directory.");
		}

		return currentDir != null 
			? Path.Combine(currentDir.FullName) 
			: Path.Combine(AppContext.BaseDirectory);
	}
}