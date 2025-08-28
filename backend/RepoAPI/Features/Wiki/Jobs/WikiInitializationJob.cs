using Quartz;
using RepoAPI.Features.Wiki.Services;
using RepoAPI.Util;

namespace RepoAPI.Features.Wiki.Jobs;

public class WikiInitializationJob(WikiDataInitService initService) : ISelfConfiguringJob
{
	public static readonly JobKey Key = new(nameof(WikiInitializationJob));

	public static void Configure(IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration)
	{
		quartz.AddJob<WikiInitializationJob>(builder => builder.WithIdentity(Key))
			.AddTrigger(trigger => {
				trigger.ForJob(Key);
				trigger.StartAt(DateTimeOffset.Now.AddSeconds(15));
				trigger.WithSimpleSchedule(schedule =>
				{
					schedule.WithIntervalInHours(int.Parse(configuration["Jobs:WikiInitInHours"] ?? "12"));
					schedule.RepeatForever();
				});
			});
	}
	
	public async Task Execute(IJobExecutionContext context)
	{
		await initService.InitializeWikiDataIfNeededAsync(context.CancellationToken);
	}
}