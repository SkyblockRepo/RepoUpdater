namespace RepoAPI.Features.Output.Services;

using System.Threading.Channels;

public record EntityWriteRequest(string Path, object Data);

[RegisterService<JsonWriteQueue>(LifeTime.Singleton)]
public class JsonWriteQueue
{
	private readonly Channel<EntityWriteRequest> _queue = Channel.CreateUnbounded<EntityWriteRequest>();

	// Create a channel that can hold an unlimited number of items

	// Method to add a request to the queue
	public async ValueTask QueueWriteAsync(EntityWriteRequest request)
	{
		await _queue.Writer.WriteAsync(request);
	}

	// Method for the background service to read from the queue
	public IAsyncEnumerable<EntityWriteRequest> ReadAllAsync(CancellationToken ct = default)
	{
		return _queue.Reader.ReadAllAsync(ct);
	}
}