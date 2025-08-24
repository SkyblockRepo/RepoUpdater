using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace RepoAPI.Data;

public static class CacheConfiguration
{
	public static WebApplicationBuilder AddCacheConfiguration(this WebApplicationBuilder builder)
	{
		builder.Services.AddHybridCache(options =>
		{
			options.DefaultEntryOptions = new HybridCacheEntryOptions()
			{
				Expiration = TimeSpan.FromMinutes(1),
				LocalCacheExpiration = TimeSpan.FromSeconds(20)
			};
		});
		
		if (builder.Environment.IsEnvironment("Testing"))
		{
			return builder;
		}
		
		var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6380";
		try {
			var multiplexer = ConnectionMultiplexer.Connect(redisConnection);
			builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
		
			builder.Services.AddOutputCache(options => {
				options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);
			});
		
			builder.Services.AddStackExchangeRedisOutputCache(options => {
				options.Configuration = redisConnection;
				options.InstanceName = "SkyblockRepo-OutputCache";
			});
		
			builder.Services.AddStackExchangeRedisCache(options => {
				options.Configuration = redisConnection;
				options.InstanceName = "SkyblockRepo";
			});
		} catch (Exception ex) {
			throw new InvalidOperationException("Failed to connect to Redis.", ex);
		}
		
		return builder;
	}
}