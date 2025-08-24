namespace RepoAPI.Features.Wiki.Responses;

using System.Text.Json.Serialization;

public class MainSlot
{
	[JsonPropertyName("*")]
	public required string Content { get; set; }
}

public class Slots
{
	public required MainSlot Main { get; set; }
}

public class Revision
{
	public required Slots Slots { get; set; }
}

public class Page
{
	public int Pageid { get; set; }
	public int Ns { get; set; }
	public required string Title { get; set; }
	public required List<Revision> Revisions { get; set; }
}

public class Query
{
	public required Dictionary<string, Page> Pages { get; set; }
}

public class WikiApiResponse
{
	public required Query Query { get; set; }
}