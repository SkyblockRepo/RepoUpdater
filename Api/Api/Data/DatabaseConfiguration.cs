namespace Api.Data;

public static class DatabaseConfiguration
{
	public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services)
	{
		services.AddDbContext<DataContext>();
		
		return services;
	}
}