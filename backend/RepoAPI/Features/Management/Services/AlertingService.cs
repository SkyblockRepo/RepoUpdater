namespace RepoAPI.Features.Management.Services;

public interface IAlertingService
{
	Task SendPendingChangeAlertAsync(int batchId, CancellationToken ct = default);
}

[RegisterService<IAlertingService>(LifeTime.Scoped)]
public class AlertingService : IAlertingService
{

	public async Task SendPendingChangeAlertAsync(int batchId, CancellationToken ct = default)
	{
		var subject = $"[RepoAPI] Pending Change Alert - Batch {batchId}";
		var body = $"A new batch with ID {batchId} is pending review. Please check the admin panel for details.";
	}
}