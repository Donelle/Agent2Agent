using A2A;

using Microsoft.Extensions.Http.Resilience;

using Agent2Agent.AgentB.Agents;
using Agent2Agent.AgentB.Configurations;

namespace Agent2Agent.AgentB.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));

		services.Configure<A2AClientOptions>(options =>
		{
			configuration.GetSection("AgentCard").Bind(options);
		});

		services.AddSingleton<IAgentLogicInvoker, RegistryAgentLogic>();
		services.AddSingleton<ITaskManager>(sp =>
		{
			var taskManager = new TaskManager();
			var agent = sp.GetRequiredService<IAgentLogicInvoker>();
			agent.Attach(taskManager);
			return taskManager;
		});

		services.AddTransient<Agent2AgentManager>();
	}
}
