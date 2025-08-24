using Refit;
using RepoAPI.Features.Wiki.Responses;

namespace RepoAPI.Features.Wiki.Services;

public interface IWikiApi
{
	[Get("/api.php?action=query&format=json&prop=revisions&rvprop=content&rvslots=main")]
	Task<WikiApiResponse> GetTemplateContentAsync([Query] string titles);
}