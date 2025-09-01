using Refit;
using RepoAPI.Features.Wiki.Responses;

namespace RepoAPI.Features.Wiki.Services;

public interface IWikiApi
{
	[Get("/api.php?action=query&format=json&prop=revisions&rvprop=content&rvslots=main&redirects=1")]
	Task<WikiApiResponse> GetTemplateContentAsync([Query] string titles);
	
	[Get("/api.php?action=query&list=categorymembers&cmtitle=Category:{category}&cmlimit=500&format=json")]
	Task<string> GetCategoryMembersAsync(string category, [Query] string cmcontinue = "");
}