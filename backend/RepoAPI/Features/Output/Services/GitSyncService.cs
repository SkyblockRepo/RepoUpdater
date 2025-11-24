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
    JsonWriteQueue jsonWriteQueue,
    ScriptRunnerService scriptRunnerService
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
        await PullOverridesAsync();
        
        foreach (var file in Directory.EnumerateFiles(Path.Combine(_overridesBasePath, "data"), "*.json", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(Path.Combine(_overridesBasePath, "data"), file);
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
        var source = Path.Combine(_overridesBasePath, "data", "manifest.json");
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
                
                await scriptRunnerService.ExecuteAsync(stoppingToken);
                
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

        var uri = new Uri(_config.RepositoryUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        var owner = segments[0];
        var repoName = segments[1].Replace(".git", "");

        // Configure Git User
        var botName = $"{_config.AppName}[bot]";
        var botEmail = $"{_config.AppId}+{_config.AppName}[bot]@users.noreply.github.com";
        await RunGitAsync($"config user.name \"{botName}\"", _outputBasePath);
        await RunGitAsync($"config user.email \"{botEmail}\"", _outputBasePath);

        // Configure Remote with Token
        var token = await serviceProvider.GetRequiredService<IGitHubTokenService>().GetTokenAsync();
        var patUrl = _config.RepositoryUrl.Replace(
            "https://",
            $"https://x-access-token:{token}@"
        );
        await RunGitAsync($"remote set-url origin {patUrl}", _outputBasePath);

        // Fetch latest changes from the remote
        await RunGitAsync("fetch origin", _outputBasePath);

        // Determine which branch to use
        string branchName;
        bool isNewBranch = false;

        try
        {
            // Check for any open PRs from the bot
            var pullRequests = await client.PullRequest.GetAllForRepository(owner, repoName, new PullRequestRequest
            {
                State = ItemStateFilter.Open,
                Base = _config.MainBranch
            });

            // Find a PR that looks like one of ours (starts with automated-sync)
            var existingPr = pullRequests.FirstOrDefault(pr => pr.Head.Ref.StartsWith("automated-sync"));

            if (existingPr != null)
            {
                branchName = existingPr.Head.Ref;
                logger.LogInformation("Found existing open PR #{Number} on branch '{Branch}'. Continuing work on this branch.", existingPr.Number, branchName);
            }
            else
            {
                // No open PR, start a fresh branch
                branchName = $"automated-sync-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                isNewBranch = true;
                logger.LogInformation("No existing open PR found. Starting new branch '{Branch}'.", branchName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query GitHub for existing PRs. Falling back to default branch behavior.");
            branchName = _config.TargetBranch;
        }

        // Switch to the target branch
        if (isNewBranch)
        {
            // Create new branch from origin/main
            await RunGitAsync($"checkout -B {branchName} origin/{_config.MainBranch}", _outputBasePath);
        }
        else
        {
            // Checkout existing branch
            // Check if it exists locally first
            var (_, localBranches) = await RunGitAsync("branch --list", _outputBasePath);
            var branchExistsLocally = localBranches
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Any(b => b.Trim().EndsWith(branchName));

            if (branchExistsLocally)
            {
                await RunGitAsync($"checkout {branchName}", _outputBasePath);
                // Ensure we are up to date with remote
                await RunGitAsync($"pull origin {branchName}", _outputBasePath);
            }
            else
            {
                // Exists on remote (because we found a PR), but not locally. Checkout from remote.
                await RunGitAsync($"checkout -B {branchName} origin/{branchName}", _outputBasePath);
            }
        }
        
        // Stage all changes
        await RunGitAsync("add -A", _outputBasePath);

        // Check if there’s anything to commit
        var (_, statusOutput) = await RunGitAsync("status --porcelain", _outputBasePath);
        if (string.IsNullOrWhiteSpace(statusOutput))
        {
            logger.LogInformation("No changes detected, skipping commit.");
            return;
        }

        var changes = ParseGitStatus(statusOutput);
        var commitMessage = GenerateCommitMessage(changes);
        
        // Only commit if there are changes
        var (commitSuccess, commitOutput) = await RunGitAsync(
            $"commit -m \"{commitMessage}\"",
            _outputBasePath
        );

        if (!commitSuccess)
        {
            logger.LogError("Commit failed. Output:\n{Output}", commitOutput);
            return;
        }

        logger.LogInformation("Committed changes to branch '{Branch}'.", branchName);
        
        // Push changes
        var (pushSuccess, pushOutput) = await RunGitAsync($"push -u origin {branchName}", _outputBasePath);

        if (pushSuccess)
        {
            logger.LogInformation("Successfully pushed changes to branch '{Branch}'.", branchName);
            
            if (isNewBranch)
            {
                var prTitle = GeneratePrTitle(changes);
                var prBody = GeneratePrBody(changes);
                await CreatePullRequestIfNeededAsync(client, branchName, _config.MainBranch, prTitle, prBody);
            }
        }
        else
        {
            logger.LogError("Failed to push changes. \nOutput:\n{Output}", pushOutput);
        }
    }
    
    private async Task PullOverridesAsync()
    {
        if (!Directory.Exists(_overridesBasePath))
        {
            logger.LogWarning("Overrides directory does not exist at path: {Path}", _overridesBasePath);
            return;
        }

        var (pullSuccess, pullOutput) = await RunGitAsync("pull origin main", _overridesBasePath);
        if (!pullSuccess)
        {
            logger.LogError("Failed to pull overrides. Output:\n{Output}", pullOutput);
        }
        else
        {
            logger.LogInformation("Successfully pulled latest overrides.");
        }
    }

    private async Task CreatePullRequestIfNeededAsync(IGitHubClient client, string sourceBranch, string targetBranch, string title, string body)
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

            var newPullRequest = new NewPullRequest(title, sourceBranch, targetBranch)
            {
                Body = body
            };
            
            var createdPullRequest = await client.PullRequest.Create(owner, repoName, newPullRequest);
            logger.LogInformation("Successfully created pull request #{Number}: {Url}", createdPullRequest.Number, createdPullRequest.HtmlUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create pull request for branch {Branch}", sourceBranch);
        }
    }

    private List<string> ParseGitStatus(string statusOutput)
    {
        var files = new List<string>();
        using var reader = new StringReader(statusOutput);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 4) continue;
            // Format is "XY PATH". We just want the path, trimmed.
            var path = line.Substring(3).Trim();
            files.Add(path);
        }
        return files;
    }

    private string GenerateCommitMessage(List<string> files)
    {
        if (files.Count == 1)
        {
            return $"Update {Path.GetFileName(files[0])}";
        }
        
        if (files.Count <= 3)
        {
            var names = files.Select(Path.GetFileName);
            return $"Update {string.Join(", ", names)}";
        }

        return $"Update {files.Count} files (including {Path.GetFileName(files[0])})";
    }

    private string GeneratePrTitle(List<string> files)
    {
        return GenerateCommitMessage(files);
    }

    private string GeneratePrBody(List<string> files)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Automated changes synced at `{DateTime.UtcNow:F}`.");
        sb.AppendLine();
        sb.AppendLine("### Changes:");
        
        // List up to 10 files
        foreach (var file in files.Take(10))
        {
            sb.AppendLine($"- `{file}`");
        }
        
        if (files.Count > 10)
        {
            sb.AppendLine($"- ... and {files.Count - 10} more.");
        }
        
        sb.AppendLine();
        sb.AppendLine("Please review and merge.");
        return sb.ToString();
    }

    private async Task<(bool Success, string Output)> RunGitAsync(string arguments, string workingDir)
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
        
        if (process.ExitCode != 0)
        {
            logger.LogInformation("Git command 'git {Arguments}' failed with exit code {ProcessExitCode}.", arguments, process.ExitCode);
            return (false, output.ToString());
        }
        
        return (true, output.ToString());
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