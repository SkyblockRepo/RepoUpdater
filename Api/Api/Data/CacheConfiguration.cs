namespace Api.Data;

public static class CacheConfiguration
{
	public static IServiceCollection AddCacheConfiguration(this IServiceCollection services)
	{
		services.AddDbContext<DataContext>();
		
		return services;
	}
}