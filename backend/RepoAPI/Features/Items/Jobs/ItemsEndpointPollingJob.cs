using Quartz;
using RepoAPI.Features.Items.Services;
using RepoAPI.Util;

namespace RepoAPI.Features.Items.Jobs;

public class ItemsEndpointPollingJob(ItemsIngestionService ingestionService) : ISelfConfiguringJob
{
	public static readonly JobKey Key = new(nameof(ItemsEndpointPollingJob));

	public static void Configure(IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration)
	{
		quartz.AddJob<ItemsEndpointPollingJob>(builder => builder.WithIdentity(Key))
			.AddTrigger(trigger => {
				trigger.ForJob(Key);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(2));
				trigger.WithSimpleSchedule(schedule =>
				{
					schedule.WithIntervalInSeconds(int.Parse(configuration["Jobs:ItemsPollingIntervalSeconds"] ?? "300"));
					schedule.RepeatForever();
				});
			});
	}
	
	public async Task Execute(IJobExecutionContext context)
	{
		await ingestionService.IngestItemsDataAsync();
	}
}