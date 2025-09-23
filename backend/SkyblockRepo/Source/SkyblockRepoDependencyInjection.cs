using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SkyblockRepo;

public static class SkyblockRepoDependencyInjection
{
	public static IServiceCollection AddSkyblockRepo(this IServiceCollection services, SkyblockRepoConfiguration? options = null) {
		options ??= new SkyblockRepoConfiguration();
		services.AddSingleton(options);
		
		services.AddSingleton<ISkyblockRepoUpdater>(sp => 
		{
			var config = sp.GetRequiredService<SkyblockRepoConfiguration>();
			var factory = sp.GetRequiredService<IHttpClientFactory>();
			var logger = sp.GetService<ILogger<SkyblockRepoUpdater>>();
			var repoLogger = sp.GetService<ILogger<GithubRepoUpdater>>();
			return new SkyblockRepoUpdater(config, factory, logger, repoLogger);
		});
		
		services.AddSingleton<ISkyblockRepoClient, SkyblockRepoClient>();
		return services;
	}

	public static IServiceCollection AddSkyblockRepo(this IServiceCollection services,
		Action<SkyblockRepoConfiguration> options)
	{
		var config = new SkyblockRepoConfiguration();
		options(config);
		return services.AddSkyblockRepo(config);
	}
}