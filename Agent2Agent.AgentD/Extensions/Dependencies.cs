
using A2A;

using Agent2Agent.AgentD.Configurations;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Agent2Agent.AgentD.Extensions;

public static class Dependencies
{
    public static IServiceCollection AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
        services.AddHttpClient();

		    services.Configure<A2AClientOptions>(options =>
		    {
			    configuration.GetSection("AgentCard").Bind(options);
		    });

		    services.AddSingleton<IAgentLogicInvoker, InternetSearchAgentLogic>();
		    services.AddSingleton<ITaskManager>(sp =>
		    {
			    var taskManager = new TaskManager();
			    var agent = sp.GetRequiredService<IAgentLogicInvoker>();
			    agent.Attach(taskManager);
			    return taskManager;
		    });

		    services.AddTransient(sp =>
            new ChatCompletionAgent
            {
                Name = "InternetSearchAgent",
                Instructions = """
                    You are Internet Search Agent, a helpful internet search assistant.
                    You will find information on the internet based on the user's input.
                    Include relevant details and sources in your response.
                    If you cannot find information, respond with "I could not find any relevant information."                    
                    Use the following format for your response and include all relevant information:
                    - Title: [Title of the information]
                    - Content: [Content of the information]
                    - Sources: [URL or source of the information]
                    Ensure your response is formated as Markdown.
                    If the user asks for a specific topic, focus on that topic.
                    If the user asks for a summary, provide a concise summary of the information found.
                    If the user asks for a list, provide a list of relevant items.                    
                    If the user asks for a definition, provide a clear and concise definition.
                    If the user asks for an explanation, provide a detailed explanation.
                    If the user asks for a step-by-step guide, provide a clear and concise guide.
                    If the user asks for a how-to, provide a clear and concise how-to.
                """,
                Kernel = Kernel.CreateBuilder()
						    .AddOpenAIChatCompletion(configuration["OpenAI:ModelId"] ?? string.Empty, configuration["OpenAI:ApiKey"] ?? string.Empty)
						    .Build()
						}
        );

        return services;
    }
}
