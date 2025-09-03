namespace RepoAPI.Features.Output.Services;

using System.Threading.Channels;

public record EntityWriteRequest(string Path, object Data, List<string>? KeepProperties = null, bool MergeInto = false);

[RegisterService<JsonWriteQueue>(LifeTime.Singleton)]
public class JsonWriteQueue
{
	private readonly Channel<EntityWriteRequest> _queue = Channel.CreateUnbounded<EntityWriteRequest>();
	
	public async ValueTask QueueWriteAsync(EntityWriteRequest request)
	{
		await _queue.Writer.WriteAsync(request);
	}

	public IAsyncEnumerable<EntityWriteRequest> ReadAllAsync(CancellationToken ct = default)
	{
		return _queue.Reader.ReadAllAsync(ct);
	}
	
	public bool IsEmpty => _queue.Reader.Count == 0;
}