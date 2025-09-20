namespace RepoAPI.Features.Wiki.Responses;

public class FormattedWikiResponse
{
	public List<QueryNormalization> Normalized { get; set; } = [];
	public Dictionary<string, FormattedPage> Pages { get; set; } = new();
}

public class FormattedPage
{
	public int Pageid { get; set; }
	public int Ns { get; set; }
	public required string Title { get; set; }
	public string Content { get; set; }
}