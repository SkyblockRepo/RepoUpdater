namespace RepoAPI.Core.Models;

public interface IVersionedEntity
{
	public int Id { get; }
	
	public string InternalId { get; set; }
	
	public bool Latest { get; set; }
	
	public DateTimeOffset IngestedAt { get; set; }
}