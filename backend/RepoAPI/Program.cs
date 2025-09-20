using System.Net;
using System.Text.Json;
using EliteFarmers.HypixelAPI;
using RepoAPI.Data;
using RepoAPI.Util;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.HttpOverrides;
using Quartz;
using SkyblockRepo;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

var builder = WebApplication.CreateBuilder();
var services = builder.Services;

services.AddFastEndpoints(o =>
{
	o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});
services.AddSwaggerDocumentation();

builder.AddDatabaseConfiguration();
services.RegisterServicesFromRepoAPI();
services.AddGitHubClient();
services.AddSelfConfiguringServices(builder.Configuration);

services.Configure<JsonOptions>(o =>
{
	o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	o.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

var userAgent = builder.Configuration["RefitSettings:UserAgent"] ?? "RepoAPI/1.0 (+https://skyblockrepo.com/)";

services.AddHypixelApi(opt =>
	{
		opt.ApiKey = builder.Configuration["HypixelApiKey"] ?? string.Empty;
		opt.UserAgent = userAgent;
	})
	.AddHttpMessageHandler<LoggingDelegatingHandler>()
	.AddStandardResilienceHandler();

builder.AddWikiClient();

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

services.AddSkyblockRepo();

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
	await app.InitializeDatabaseAsync();
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
