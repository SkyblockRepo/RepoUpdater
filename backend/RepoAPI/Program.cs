using System.Net;
using System.Text.Json;
using RepoAPI.Data;
using RepoAPI.Util;
using HypixelAPI;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Quartz;
using Refit;
using RepoAPI.Features.Wiki.Services;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder();
var services = builder.Services;

services.AddFastEndpoints(o =>
{
	o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});
services.AddSwaggerDocumentation();

services.AddDatabaseConfiguration();
services.RegisterServicesFromRepoAPI();
services.AddSelfConfiguringServices(builder.Configuration);

services.Configure<JsonOptions>(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

services.AddRefitClient<IWikiApi>()
	.ConfigureHttpClient(c =>
	{
		c.BaseAddress = new Uri("https://wiki.hypixel.net/");
		c.DefaultRequestHeaders.Add("User-Agent", "RepoAPI");
		c.Timeout = TimeSpan.FromSeconds(10);
	})
	.AddStandardResilienceHandler();

services.AddHypixelApi(builder.Configuration["HypixelApiKey"] ?? string.Empty, "RepoAPI")
	.AddStandardResilienceHandler();

services.AddQuartz(q =>
{
	q.UseInMemoryStore();
	q.AddSelfConfiguringJobs(builder.Configuration);
});

services.AddQuartzHostedService(options => {
	options.WaitForJobsToComplete = true;
});

// Use Cloudflare IP address as the client remote IP address
builder.Services.Configure<ForwardedHeadersOptions>(opt => {
	opt.ForwardedForHeaderName = "CF-Connecting-IP";
	opt.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
	// Safe because we only allow Cloudflare to connect to the API through the firewall
	opt.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
	opt.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
});

builder.AddCacheConfiguration();

var app = builder.Build();

app.UseResponseCaching();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseKnownBotDetection();

app.UseFastEndpoints(c =>
{
	c.Binding.ReflectionCache.AddFromRepoAPI();
	
	c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	c.Serializer.Options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

	c.Versioning.Prefix = "v";
	c.Versioning.DefaultVersion = 1;
	c.Versioning.PrependToRoute = true;
});

app.UseOpenApiConfiguration();
app.UseOutputCache();

if (!app.Environment.IsTesting()) {
	await app.MigrateDatabase();
}

app.Run();

internal sealed class PingEndpoint : EndpointWithoutRequest
{
	public override void Configure()
	{
		Get("/ping");
		AllowAnonymous();
	}

	public override async Task HandleAsync(CancellationToken ct)
	{
		await Send.OkAsync("pong", cancellation: ct);
	}
}
