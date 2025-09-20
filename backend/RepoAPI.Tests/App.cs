using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RepoAPI.Data;
using RepoAPI.Features.Wiki.Services;

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
		s.RemoveAll<IHostedService>();

		s.AddOutputCache();
		s.AddMemoryCache();
		s.AddDistributedMemoryCache();
	}
}

class FakeWebHostEnvironment : IWebHostEnvironment
{
	public string EnvironmentName { get; set; } = "Testing";
	public string ApplicationName { get; set; } = "Tests";
	public string WebRootPath { get; set; } = string.Empty;
	public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
	public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
	public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}