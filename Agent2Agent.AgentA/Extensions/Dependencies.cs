using System.Text.Json;

using A2A;

using Agent2Agent.AgentA.Plugins;
using Agent2Agent.AgentA.Services;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Agent2Agent.AgentA.Extensions;

public static class Dependencies
{
	public static void AddAgentDependencies(this IServiceCollection services, IConfiguration configuration)
	{
		services.AddProblemDetails();
		services.AddLogging(o => o.AddDebug().SetMinimumLevel(LogLevel.Trace));
		services.AddHttpClient();
		services.AddControllers();
		services.AddStackExchangeRedisCache(options =>
		{
			options.Configuration = configuration["Redis:ConnectionString"];
			options.InstanceName = "AgentA";
		});

		services.AddSingleton<IAgentCacheProvider, AgentCacheProvider>();
		services.AddSingleton<IConversationService, ConversationService>();

		services.AddTransient(sp =>
		{
			var logfactory = sp.GetRequiredService<ILoggerFactory>();
			var agentProvider = sp.GetRequiredService<IAgentCacheProvider>();
			var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

			var kernel = Kernel.CreateBuilder()
					.AddOpenAIChatCompletion(
						configuration["OpenAI:ModelId"] ?? string.Empty,
						configuration["OpenAI:ApiKey"] ?? string.Empty
			).Build();

			var functions = agentProvider.Agents.Select(a =>
			{
				var client = new A2AClient(new Uri(a.Uri), httpClientFactory.CreateClient("A2AClient"));
				var plugin = new CustomerAdvocatePlugin(client, logfactory.CreateLogger<CustomerAdvocatePlugin>());

				return a.Skills.Select(a =>
				{
					var description = a.Description;
					if (a.Prompts.Count > 0)
						description += "\nExample Prompts:\n".Concat(string.Join("\n", a.Prompts));

					var opts = new KernelFunctionFromMethodOptions
					{
						Description = description,
						FunctionName = a.Name,
						LoggerFactory = logfactory,
					};

					return KernelFunctionFactory.CreateFromMethod(
						plugin.GetType().GetMethod(nameof(plugin.FetchAsync))!, JsonSerializerOptions.Default, plugin, opts);
				});
			}).SelectMany(a => a);

			kernel.Plugins.AddFromFunctions("cas", functions);

			return new ChatCompletionAgent
			{
				Name = "CustomerAdvocateAssistant",
				Instructions = """
                    You are Customer Advocate Agent, a customer service assistant.\n
                    Your role is to assist customers with their automotive inquiries.\n
                    You will provide information about automotive products, services, registrations, and policies.\n

                    # Things you should ALWAYS do:\n
                    - Always use the provided tools and functions to fetch information\n
                    - Always use the knowledge base tool first then use the internet tool second to find information.\n
                    - Always validate that inquiries are related to automotive topics before proceeding.
                    Once validated, forward the entire inquiry to the required function and/or tool for processing.\n
                    - Always ask the user to clarify their inquiry if you are unsure about the topic.\n
                    - Always document the inquiry and the steps taken to resolve it.\n

                    # Things you should NEVER do:\n
                    - Never provide any other information if it is not related to automotive inquiries.\n
                    - Never answer if information is out of your scope.\n

                    # Operational Guidelines\n
                    - If information is not available in the knowledge base, escalate the inquiry.\n
                    - If you need to access external information, use the internet tool as a last resort.\n

                """,
				Kernel = kernel,
				LoggerFactory = sp.GetRequiredService<ILoggerFactory>(),
				Arguments = new KernelArguments(new OpenAIPromptExecutionSettings
				{
					FunctionChoiceBehavior = FunctionChoiceBehavior.Required(functions)
				})
			};
		});
	}
}
