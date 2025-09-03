using System.Reflection;
using Microsoft.Extensions.Options;
using Octokit;
using Quartz;
using RepoAPI.Features.Output.Services;

namespace RepoAPI.Util;

public static class GitHubClientExtensions
{
	public static void AddGitHubClient(this IServiceCollection services)
	{
		services.AddTransient<IGitHubClient>(provider =>
		{
			var tokenService = provider.GetRequiredService<IGitHubTokenService>();
			var token = tokenService.GetTokenAsync().GetAwaiter().GetResult();

			return new GitHubClient(new ProductHeaderValue("RepoAPI-GitSyncService"))
			{
				Credentials = new Credentials(token)
			};
		});
	}
}