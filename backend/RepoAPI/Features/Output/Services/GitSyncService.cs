using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Octokit;
using RepoAPI.Util;

namespace RepoAPI.Features.Output.Services;

public class GitSyncService(
    ILogger<GitSyncService> logger,
    IOptions<GitSyncOptions> gitSyncOptions,
    IServiceProvider serviceProvider,
    JsonWriteQueue jsonWriteQueue
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
    
    private async Task ApplyOverridesAsync(CancellationToken token)
    {
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_overridesBasePath, "data"), "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(Path.Combine(_overridesBasePath, "data"), file);
            var dest = Path.Combine(Path.Combine(_overridesBasePath, "data"), relative);

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
        var exclusionsFile = Path.Combine(_overridesBasePath, "data", "exclusions.txt");
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(45), stoppingToken); // Initial delay to allow other services to start
        logger.LogInformation("Git Sync Service started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            if (JsonWriteQueue.WasRecentlyQueued()) {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                continue; // Wait until the write queue is empty before proceeding
            }
            
            using var scope = serviceProvider.CreateScope();
            
            var scopedServiceProvider = scope.ServiceProvider;
            var gitHubClient = scopedServiceProvider.GetRequiredService<IGitHubClient>();

            try {
                logger.LogInformation("Git Sync Service cycle started.");
                await ApplyOverridesAsync(stoppingToken);
                ApplyExclusions();
                CopyManifest();
                
                await CommitAndPushAsync(gitHubClient);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during the git sync cycle.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
    
    private async Task CommitAndPushAsync(IGitHubClient client)
    {
        if (!_config.PushEnabled)
        {
            logger.LogInformation("Push is disabled. Skipping commit and push.");
            return;
        }

        var branch = _config.TargetBranch;

        var botName = $"{_config.AppName}[bot]";
        var botEmail = $"{_config.AppId}+{_config.AppName}[bot]@users.noreply.github.com";
        await RunGitAsync($"config user.name \"{botName}\"", _outputBasePath);
        await RunGitAsync($"config user.email \"{botEmail}\"", _outputBasePath);

        var token = await serviceProvider.GetRequiredService<IGitHubTokenService>().GetTokenAsync();
        var patUrl = _config.RepositoryUrl.Replace(
            "https://",
            $"https://x-access-token:{token}@"
        );
        await RunGitAsync($"remote set-url origin {patUrl}", _outputBasePath);

        // Fetch and check out branch
        await RunGitAsync("fetch origin", _outputBasePath);
        
        // Create or reset the target branch from the latest main branch.
        await RunGitAsync($"checkout -B {branch} origin/{_config.MainBranch}", _outputBasePath);

        await RunGitAsync($"branch --set-upstream-to=origin/{branch} {branch}", _outputBasePath);

        // Stage
        await RunGitAsync("add .", _outputBasePath);

        // Check if there’s anything to commit
        var (statusExitCode, statusOutput) = await RunGitAsync("status --porcelain", _outputBasePath);
        if (statusExitCode != 0)
        {
            logger.LogWarning("Failed to check git status. Output:\n{Output}", statusOutput);
        }

        if (!string.IsNullOrWhiteSpace(statusOutput))
        {
            // Only commit if there are changes
            var (commitExitCode, commitOutput) = await RunGitAsync(
                $"commit -m \"Automated Sync {DateTime.UtcNow:O}\"",
                _outputBasePath
            );

            if (commitExitCode != 0)
            {
                logger.LogError("Commit failed. Output:\n{Output}", commitOutput);
                return;
            }

            logger.LogInformation("Committed changes to branch '{Branch}'.", branch);
        } else {
            logger.LogInformation("No changes detected, skipping commit.");
            return;
        }

        var (pushExitCode, pushOutput) = await RunGitAsync($"push -u origin {branch} --force", _outputBasePath);

        if (pushExitCode == 0)
        {
            logger.LogInformation("Successfully pushed changes to branch '{Branch}'.", branch);
            await CreatePullRequestIfNeededAsync(client, branch, _config.MainBranch);
        }
        else
        {
            logger.LogError("Failed to push changes. ExitCode={Code}\nOutput:\n{Output}", 
                pushExitCode, pushOutput);
        }
    }

    private async Task CreatePullRequestIfNeededAsync(IGitHubClient client, string sourceBranch, string targetBranch)
    {
        try
        {
            var uri = new Uri(_config.RepositoryUrl);
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            var owner = segments[0];
            var repoName = segments[1].Replace(".git", "");
            
            var pullRequests = await client.PullRequest.GetAllForRepository(owner, repoName, new PullRequestRequest
            {
                State = ItemStateFilter.Open,
                Head = $"{owner}:{sourceBranch}",
                Base = targetBranch
            });
            
            if (pullRequests.Count > 0)
            {
                logger.LogInformation("An open pull request already exists for branch '{Branch}'. Skipping creation.", sourceBranch);
                return;
            }

            var newPullRequest = new NewPullRequest($"Automated Sync: {sourceBranch}", sourceBranch, targetBranch)
            {
                Body = $"Automated changes synced at `{DateTime.UtcNow:F}`. Please review and merge."
            };
            
            var createdPullRequest = await client.PullRequest.Create(owner, repoName, newPullRequest);
            logger.LogInformation("Successfully created pull request #{Number}: {Url}", createdPullRequest.Number, createdPullRequest.HtmlUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create pull request for branch {Branch}", sourceBranch);
        }
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
    public string RepositoryUrl { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public long AppId { get; set; }
    public string TargetBranch { get; set; } = "automated-sync";
    public string MainBranch { get; set; } = "main";
    public string? PrivateKeyPath { get; set; } // Path to your .pem file
    public string? PrivateKey { get; set; } // Or the key content directly
}