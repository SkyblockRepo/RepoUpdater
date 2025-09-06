using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Octokit;

namespace RepoAPI.Features.Output.Services;

public interface IGitHubTokenService
{
    Task<string> GetTokenAsync();
}

[RegisterService<IGitHubTokenService>(LifeTime.Singleton)]
public class GitHubTokenService(
    ILogger<GitHubTokenService> logger,
    IOptions<GitSyncOptions> gitSyncOptions
) : IGitHubTokenService, IDisposable
{
    private readonly GitSyncOptions _config = gitSyncOptions.Value;
    private string? _token;
    private DateTimeOffset _tokenExpiration;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private RSA? _rsa;
    private SigningCredentials? _credentials;

    public async Task<string> GetTokenAsync()
    {
        // Prevent multiple threads from trying to refresh the token simultaneously.
        await _semaphore.WaitAsync();
        try
        {
            // Don't refresh the token if it's null, or if it's about to expire (e.g., within the next 5 minutes).
            if (_token is not null && DateTimeOffset.UtcNow < _tokenExpiration.AddMinutes(-5)) return _token!;

            logger.LogInformation("GitHub App token is expired or nearing expiration. Generating a new one...");
            await GenerateNewTokenAsync();
            return _token!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate a new token.");
            return _token!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task GenerateNewTokenAsync()
    {
        var jwt = GenerateJwtToken();
        var appClient = new GitHubClient(new ProductHeaderValue(_config.AppName))
        {
            Credentials = new Credentials(jwt, AuthenticationType.Bearer)
        };
        
        var authenticatedApp = await appClient.GitHubApps.GetCurrent();
        logger.LogInformation("Authenticated as GitHub App: {AppName} (ID: {AppId})", authenticatedApp.Name, authenticatedApp.Id);

        // Find the Installation ID for the target repository.
        var installationId = await FindInstallationIdAsync(appClient);

        // Use the Installation ID to create the installation access token.
        var installationToken = await appClient.GitHubApps.CreateInstallationToken(installationId);

        _token = installationToken.Token;
        _tokenExpiration = installationToken.ExpiresAt;
        
        logger.LogInformation("Successfully generated new GitHub App installation token. Valid until: {Expiration}", _tokenExpiration);
    }
    
    private async Task<long> FindInstallationIdAsync(GitHubClient appClient)
    {
        var uri = new Uri(_config.RepositoryUrl);
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        var owner = segments[0];
        var repoName = segments[1].Replace(".git", "");
        
        try
        {
            var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(owner, repoName);
            logger.LogInformation("Found installation ID {Id} for repository {Owner}/{Repo}", installation.Id, owner, repoName);
            return installation.Id;
        }
        catch (NotFoundException)
        {
            logger.LogError("Could not find a GitHub App installation for {Owner}/{Repo}. Ensure the app is installed and has access to this repository.", owner, repoName);
            throw;
        }
    }

    private string GenerateJwtToken()
    {
        _credentials ??= CreateSigningCredentials();
        
        var handler = new JsonWebTokenHandler();
        var now = DateTime.UtcNow;
        
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config.AppId.ToString(),
            IssuedAt = now,
            Expires = now.AddMinutes(9),
            SigningCredentials = _credentials
        };

        return handler.CreateToken(descriptor);
    }
    
    private SigningCredentials CreateSigningCredentials()
    {
        _rsa = RSA.Create();
        _rsa.ImportFromPem(GetPrivateKey());
        
        var securityKey = new RsaSecurityKey(_rsa);
        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }
    
    private string GetPrivateKey()
    {
        if (!string.IsNullOrWhiteSpace(_config.PrivateKey))
            return _config.PrivateKey;
        if (!string.IsNullOrWhiteSpace(_config.PrivateKeyPath))
            return File.ReadAllText(_config.PrivateKeyPath);
        throw new InvalidOperationException("GitHub App private key is not configured.");
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _rsa?.Dispose();
        GC.SuppressFinalize(this);
    }
}