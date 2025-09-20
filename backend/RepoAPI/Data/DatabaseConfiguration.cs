using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RepoAPI.Util;
using SkyblockRepo;

namespace RepoAPI.Data;

public static class DatabaseConfiguration
{
	public static WebApplicationBuilder AddDatabaseConfiguration(this WebApplicationBuilder builder)
	{
		builder.Services.AddDbContext<DataContext>(config =>
		{
			if (builder.Environment.IsTesting())
			{
				var c = new SqliteConnection("DataSource=:memory:");
				c.Open();
				config.UseSqlite(c);
				return;
			}
			
			var connection = builder.Configuration.GetConnectionString("Postgres");

			if (string.IsNullOrEmpty(connection))
			{
				throw new InvalidOperationException("No database connection string found.");
			}
			
			var npgsqlBuilder = new NpgsqlDataSourceBuilder(connection);
			npgsqlBuilder.EnableDynamicJson();
			var source = npgsqlBuilder.Build();

			config.UseNpgsql(source,
				opt => { opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery); });
		});

		return builder;
	}

	public static async Task InitializeDatabaseAsync(this WebApplication app)
	{
		if (app.Environment.IsTesting()) return;

		using var scope = app.Services.CreateScope();
		var logging = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
		logging.LogInformation("Starting RepoAPI...");
		
		var repo = scope.ServiceProvider.GetRequiredService<ISkyblockRepoClient>();
		await repo.InitializeAsync();

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