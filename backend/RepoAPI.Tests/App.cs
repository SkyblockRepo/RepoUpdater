using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepoAPI.Data;

namespace RepoAPI.Tests;

public class App : AppFixture<Program>
{
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
		var dataContextImpl = s.SingleOrDefault(d => d.ServiceType == typeof(DataContext));
		if (dataContextImpl != null)
		{
			s.Remove(dataContextImpl);
		}
		s.AddDbContext<DataContext>(options =>
		{
			options.UseInMemoryDatabase(Guid.NewGuid().ToString());
		});
		
		s.AddMemoryCache();
		s.AddDistributedMemoryCache();
		s.AddOutputCache();
	}
}