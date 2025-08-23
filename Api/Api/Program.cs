global using FastEndpoints;
global using FluentValidation;
using Api.Data;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

var bld = WebApplication.CreateBuilder();
var services = bld.Services;

services.AddFastEndpoints();

services.AddDatabaseConfiguration();
services.AddCacheConfiguration();

var app = bld.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseFastEndpoints();
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