using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace SkyblockRepo;

public static class SkyblockRepoDependencyInjection
{
	public static IServiceCollection AddSkyblockRepo(this IServiceCollection services, SkyblockRepoConfiguration? options = null) {
		options ??= new SkyblockRepoConfiguration();
		services.AddSingleton(options);
		services.AddSingleton<ISkyblockRepoUpdater, SkyblockRepoUpdater>();
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