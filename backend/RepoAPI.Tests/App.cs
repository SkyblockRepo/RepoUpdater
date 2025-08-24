using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepoAPI.Data;

namespace RepoAPI.Tests;

public class App : AppFixture<Program>
{
	private DbConnection? _connection;
	
	protected override void ConfigureApp(IWebHostBuilder a)
	{
		a.UseEnvironment("Testing");
		
		a.ConfigureAppConfiguration((_, conf) =>
		{
			conf.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:Postgres"] = "Host=localhost;Database=test-db",
				["ConnectionStrings:Redis"] = "localhost:6379"
			});
		});
	}

	protected override void ConfigureServices(IServiceCollection s)
	{
		var dbContextDescriptor = s.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DataContext>));
		if (dbContextDescriptor != null)
		{
			s.Remove(dbContextDescriptor);
		}

		// Create an open SQLite connection
		_connection = new SqliteConnection("DataSource=:memory:");
		_connection.Open();

		// Add the DbContext with the SQLite provider
		s.AddDbContext<DataContext>(options =>
		{
			options.UseSqlite(_connection);
		});
		
		s.AddMemoryCache();
		s.AddDistributedMemoryCache();
		s.AddOutputCache();
	}
}