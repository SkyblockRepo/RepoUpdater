global using FastEndpoints;
global using FluentValidation;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

var bld = WebApplication.CreateBuilder();
bld.Services.AddFastEndpoints();

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