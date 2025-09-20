using RepoAPI.Features.Wiki.Responses;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Util;

namespace RepoAPI.Features.Wiki.Endpoints;

internal class GetProxyRequest
{
	public List<string> Titles { get; set; } = [];
}

internal class GetProxyEndpoint(IWikiDataService dataService) : Endpoint<GetProxyRequest, FormattedWikiResponse>
{
	public override void Configure()
	{
		Post("wiki-proxy");
		AllowAnonymous();
		
		Summary(s => {
			s.Summary = "Fetch wiki data";
			s.Description = "Internal API endpoint to fetch wiki data, only accessible by transformation scripts.";
		});
	}

	public override async Task HandleAsync(GetProxyRequest req, CancellationToken ct)
	{
		if (!HttpContext.IsFromPrivateNetwork()) {
			await Send.ForbiddenAsync(ct);
			return;
		}
		
		var response = await dataService.BatchGetData(req.Titles, ct);
		await Send.OkAsync(response, ct);
	}
}