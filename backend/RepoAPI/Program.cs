using RepoAPI.Data;
using RepoAPI.Util;

var builder = WebApplication.CreateBuilder();
var services = builder.Services;

services.AddFastEndpoints(o =>
{
	o.SourceGeneratorDiscoveredTypes = DiscoveredTypes.All;
});
services.AddSwaggerDocument();

services.AddDatabaseConfiguration();
services.RegisterServicesFromRepoAPI();

builder.AddCacheConfiguration();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseFastEndpoints(c =>
{
	c.Binding.ReflectionCache.AddFromRepoAPI();

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
