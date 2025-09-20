using RepoAPI.Util;
using SkyblockRepo;

namespace RepoAPI.Features.Output.Services;

public class RepoUpdateService(ISkyblockRepoClient repoClient) : BackgroundService, ISelfRegister
{
	private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);
	
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await Task.Delay(_interval, stoppingToken);
			await repoClient.CheckForUpdatesAsync(stoppingToken);
		}
	}

	public static void Configure(IServiceCollection services, ConfigurationManager configuration)
	{
		services.AddHostedService<RepoUpdateService>();
	}
}