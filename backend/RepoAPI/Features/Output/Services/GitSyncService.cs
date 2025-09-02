using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using RepoAPI.Util;

namespace RepoAPI.Features.Output.Services;

public class GitSyncService(
    ILogger<GitSyncService> logger,
    IOptions<GitSyncOptions> gitSyncOptions
) : BackgroundService, ISelfRegister
{
    private readonly GitSyncOptions _config = gitSyncOptions.Value;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly string _outputBasePath = GetOutputBasePath();
    private readonly string _overridesBasePath = Path.Combine(GetOutputBasePath(), "..", "overrides");

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Git Sync Service started.");
        
        // Run initial sync on startup to ensure new overrides are applied
        await ApplyOverridesAsync(stoppingToken);
        ApplyExclusions();
        CopyManifest();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await HasChangesAsync()) continue;
                
                logger.LogInformation("Detected changes in output folder. Applying overrides...");

                await ApplyOverridesAsync(stoppingToken);
                ApplyExclusions();
                CopyManifest();

                if (await HasChangesAsync())
                {
                    await CommitAndPushAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Git sync failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task<bool> HasChangesAsync()
    {
        var (exitCode, output) = await RunGitAsync("status --porcelain", _outputBasePath);
        return exitCode == 0 && !string.IsNullOrWhiteSpace(output);
    }

    private async Task ApplyOverridesAsync(CancellationToken token)
    {
        foreach (var file in Directory.EnumerateFiles(_overridesBasePath, "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(_overridesBasePath, file);
            var dest = Path.Combine(_outputBasePath, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            if (!File.Exists(dest))
            {
                File.Copy(file, dest, overwrite: true);
                logger.LogDebug("Copied new override: {File}", relative);
                continue;
            }

            try {
                // Merge with existing JSON
                var baseJson = await File.ReadAllTextAsync(dest, token);
                var overrideJson = await File.ReadAllTextAsync(file, token);

                var baseNode = JsonNode.Parse(baseJson);
                var overrideNode = JsonNode.Parse(overrideJson);

                if (baseNode != null && overrideNode != null)
                {
                    JsonUtils.MergeInto(baseNode, overrideNode);

                    var merged = JsonSerializer.Serialize(baseNode, _jsonOptions);
                    await File.WriteAllTextAsync(dest, merged, token);

                    logger.LogDebug("Merged override into: {File}", relative);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply override {File}", relative);
            }
        }
    }

    private void ApplyExclusions()
    {
        var exclusionsFile = Path.Combine(_overridesBasePath, "exclusions.txt");
        if (!File.Exists(exclusionsFile)) return;

        foreach (var line in File.ReadAllLines(exclusionsFile))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

            var target = Path.Combine(_outputBasePath, line.Trim());
            if (File.Exists(target))
            {
                File.Delete(target);
                logger.LogDebug("Deleted excluded file: {File}", line);
            }
        }
    }

    private void CopyManifest()
    {
        var source = Path.Combine(_overridesBasePath, "manifest.json");
        var dest = Path.Combine(_outputBasePath, "manifest.json");

        if (File.Exists(source))
        {
            File.Copy(source, dest, overwrite: true);
            logger.LogInformation("Updated manifest.json");
        }
    }

    private async Task CommitAndPushAsync()
    {
        // set commit author
        await RunGitAsync($"config user.name \"{_config.UserName}\"", _outputBasePath);
        await RunGitAsync($"config user.email \"{_config.UserEmail}\"", _outputBasePath);
        
        if (!string.IsNullOrWhiteSpace(_config.PersonalAccessToken) &&
            !string.IsNullOrWhiteSpace(_config.RepositoryUrl))
        {
            var patUrl = _config.RepositoryUrl.Replace(
                "https://",
                $"https://x-access-token:{_config.PersonalAccessToken}@");

            await RunGitAsync($"remote set-url origin {patUrl}", _outputBasePath);
        }
        
        var branch = string.IsNullOrWhiteSpace(_config.Branch) ? "main" : _config.Branch;
        await RunGitAsync($"checkout {branch}", _outputBasePath);
        
        await RunGitAsync("fetch origin", _outputBasePath);
        await RunGitAsync("rebase origin/main", _outputBasePath); 

        await RunGitAsync("add .", _outputBasePath);

        if (!_config.PushEnabled) {
            logger.LogInformation("Push is disabled in configuration. Skipping commit and push.");
            return;
        }
        
        var (exit, _) = await RunGitAsync($"commit -m \"Automated sync {DateTime.UtcNow:O}\"", _outputBasePath);
        if (exit != 0)
        {
            logger.LogInformation("No new changes to commit.");
            return;
        }
        
        await RunGitAsync("push origin HEAD", _outputBasePath);

        logger.LogInformation("Committed and pushed changes to git submodule as {UserName}.", _config.UserName);
    }

    private static async Task<(int ExitCode, string Output)> RunGitAsync(string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        var output = new StringBuilder();
        output.AppendLine(await process.StandardOutput.ReadToEndAsync());
        output.AppendLine(await process.StandardError.ReadToEndAsync());

        await process.WaitForExitAsync();
        return (process.ExitCode, output.ToString());
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

    public static void Configure(IServiceCollection services, ConfigurationManager config)
    {
        services.AddHostedService<GitSyncService>();
        services.Configure<GitSyncOptions>(config.GetSection("GitSync"));
    }
}

public class GitSyncOptions
{
    public bool PushEnabled { get; set; } = false;
    public string UserName { get; set; } = "github-actions[bot]";
    public string UserEmail { get; set; } = "41898282+github-actions[bot]@users.noreply.github.com";
    public string? PersonalAccessToken { get; set; }
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
}