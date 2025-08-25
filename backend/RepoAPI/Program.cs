using System.Text.Json;
using RepoAPI.Data;
using RepoAPI.Util;
using HypixelAPI;
using Microsoft.AspNetCore.Http.Json;
using Quartz;
using Refit;
using RepoAPI.Features.Wiki.Services;

var builder = WebApplication.CreateBuilder();
var services = builder.Services;

services.AddFastEndpoints(o =>
{
	o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});
services.AddSwaggerDocument();

services.AddDatabaseConfiguration();
services.RegisterServicesFromRepoAPI();
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
	});
services.AddHypixelApi(builder.Configuration["HypixelApiKey"] ?? string.Empty, "RepoAPI");

services.AddQuartz(q =>
{
	q.UseInMemoryStore();
	q.AddSelfConfiguringJobs(builder.Configuration);
});

services.AddQuartzHostedService(options => {
	options.WaitForJobsToComplete = true;
});

builder.AddCacheConfiguration();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseFastEndpoints(c =>
{
	c.Binding.ReflectionCache.AddFromRepoAPI();
	
	c.Serializer.Options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
	c.Serializer.Options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

	c.Versioning.Prefix = "v";
	c.Versioning.DefaultVersion = 1;
	c.Versioning.PrependToRoute = true;
});

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
