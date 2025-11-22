using System.Net;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Refit;
using RepoAPI.Features.Wiki.Services;

namespace RepoAPI.Util;

public static class WikiClientExtensions
{
	public static WebApplicationBuilder AddWikiClient(this WebApplicationBuilder builder)
	{
		builder.Services.AddTransient<WikiCacheHandler>();
		
		var client = builder.Services.AddRefitClient<IWikiApi>()
			.ConfigureHttpClient(c =>
			{
				c.BaseAddress = new Uri("https://wiki.hypixel.net/");
				c.Timeout = TimeSpan.FromSeconds(10);
				
				c.DefaultRequestHeaders.Add("User-Agent", builder.Configuration["RefitSettings:UserAgent"] ?? "RepoAPI/1.0 (+https://skyblockrepo.com/)");
				c.DefaultRequestHeaders.Add("Accept", "application/json");
				c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
				c.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
			})
			.ConfigurePrimaryHttpMessageHandler(() => {
				var handler = new HttpClientHandler
				{
					AutomaticDecompression = DecompressionMethods.All
				};
          
				var proxySettings = builder.Configuration.GetSection("ProxySettings");
				if (proxySettings["Uri"] is not null)
				{
					handler.Proxy = new WebProxy
					{
						Address = new Uri(proxySettings["Uri"] ?? string.Empty),
						Credentials = new NetworkCredential(proxySettings["Username"], proxySettings["Password"])
					};
				}
          
				return handler;
			})
			.AddHttpMessageHandler<WikiCacheHandler>()
			.AddHttpMessageHandler<LoggingDelegatingHandler>()
			.AddStandardResilienceHandler(opt =>
			{
				opt.Retry = new HttpRetryStrategyOptions
				{
					MaxRetryAttempts = 5,
					Delay = TimeSpan.FromSeconds(2),
					BackoffType = DelayBackoffType.Exponential
				};
			});
		
		return builder;
	}
}