using System.Diagnostics;
using System.Text;
using RepoAPI.Util;

namespace RepoAPI.Features.Output.Services;

[RegisterService<ScriptRunnerService>(LifeTime.Singleton)]
public class ScriptRunnerService(
    ILogger<ScriptRunnerService> logger
) {
    private readonly string _scriptsBasePath = Path.Combine(GetOutputBasePath(), "..", "overrides");
    
    private async Task RunScriptsAsync(CancellationToken token)
    {
        var file = Path.Combine(_scriptsBasePath, "run.ts");
        if (!File.Exists(file))
        {
            logger.LogWarning("No run.ts script found at {File}", file);
            return;
        }
        
        var (versionExitCode, version) = await RunNodeAsync("--version", _scriptsBasePath);
        if (versionExitCode != 0) {
            logger.LogError("Node.js is not installed or not found in PATH. Script execution aborted. Output: {Output}", version);
            return;
        }
        logger.LogInformation("Node.js version: {Version}", version.Trim());
        
        // Check that we're over 24.0
        var versionParts = version.Trim().Split('.');
        if (versionParts.Length < 2 || !int.TryParse(versionParts[0].TrimStart('v'), out var major) || !int.TryParse(versionParts[1], out var minor) || major < 24)
        {
            logger.LogError("Node.js version 24.0 or higher is required. Script execution aborted.");
            return;
        }
        
        var (exitCode, output) = await RunNodeAsync(file, _scriptsBasePath);
       
        if (exitCode != 0) {
            logger.LogError("Script {File} exited with code {ExitCode}. Output: {Output}", file, exitCode, output);
        } else {
            logger.LogInformation("Script {File} executed successfully. Output: {Output}", file, output);
        }
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Script runner service started.");
        
        try {
            await RunScriptsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the git sync cycle.");
        }
    }
    
    private static async Task<(int ExitCode, string Output)> RunNodeAsync(string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo("node", arguments)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try {
            using var process = Process.Start(psi)!;
            var output = new StringBuilder();
            output.AppendLine(await process.StandardOutput.ReadToEndAsync());
            output.AppendLine(await process.StandardError.ReadToEndAsync());

            await process.WaitForExitAsync();
            return (process.ExitCode, output.ToString());
        } catch (Exception ex) {
            return (-1, $"Failed to start Node.js process: {ex.Message}");
        }
    }

    private static string GetOutputBasePath()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        var iterations = 0;
        const int maxIterations = 10;

        while (currentDir != null && currentDir.GetFiles("*.sln").Length == 0)
        {
            currentDir = currentDir.Parent;
            iterations++;
            if (iterations >= maxIterations) throw new InvalidOperationException("Could not find solution root directory.");
        }

        return currentDir != null
            ? Path.Combine(currentDir.FullName, "..", "output")
            : Path.Combine(AppContext.BaseDirectory, "..", "output");
    }
}