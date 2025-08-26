using Microsoft.EntityFrameworkCore;
using RepoAPI.Util;

namespace RepoAPI.Data;

public static class DatabaseConfiguration
{
	public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services)
	{
		services.AddDbContext<DataContext>();
		
		return services;
	}

	public static async Task MigrateDatabase(this WebApplication app)
	{
		if (app.Environment.IsTesting()) return;
		
		using var scope = app.Services.CreateScope();
		var logging = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
		logging.LogInformation("Starting RepoAPI...");

		var db = scope.ServiceProvider.GetRequiredService<DataContext>();
		try
		{
			// await db.Database.EnsureDeletedAsync();
			// await db.Database.EnsureCreatedAsync();
			
			await db.Database.MigrateAsync();
		} catch (Exception e) {
			Console.Error.WriteLine(e);
		}
	}
}