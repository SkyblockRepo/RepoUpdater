using System.Reflection;
using Quartz;

namespace RepoAPI.Util;

public interface ISelfRegister
{
	/// <summary>
	/// Configures the service with the provided IServiceCollection.
	/// </summary>
	static abstract void Configure(IServiceCollection services, ConfigurationManager configuration);
}

public static class ServiceExtensions
{
	public static void AddSelfConfiguringServices(this IServiceCollection services, ConfigurationManager configuration)
	{
		// Find all types that implement ISelfRegister
		var jobTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(t => t.IsAssignableTo(typeof(ISelfRegister)) && t is { IsInterface: false, IsAbstract: false });
		
		foreach (var jobType in jobTypes)
		{
			// Find the static 'Configure' method on the job type
			var configureMethod = jobType.GetMethod(nameof(ISelfRegister.Configure), 
				BindingFlags.Public | BindingFlags.Static);
            
			if (configureMethod == null)
			{
				throw new InvalidOperationException($"Could not find static 'Configure' method on type '{jobType.Name}'.");
			}

			configureMethod.Invoke(null, [services, configuration]);
		}
	}
}