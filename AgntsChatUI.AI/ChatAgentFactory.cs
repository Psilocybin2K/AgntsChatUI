namespace AgntsChatUI.AI
{
    using System.Text.Json;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.Agents.Orchestration;
    using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
    using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.PromptTemplates.Liquid;
    using Microsoft.SemanticKernel.Prompty;

    public class ChatAgentFactory
    {
        private readonly IList<ChatAgent> _agents;
        private readonly IServiceProvider _serviceProvider;
        private InProcessRuntime? _runtime;

        public ChatAgentFactory()
        {
            this._agents = [.. LoadAgentsFromConfig()];
            this._serviceProvider = ConfigureServices();
        }

        private static IServiceProvider ConfigureServices()
        {
            IHostBuilder builder = Host.CreateDefaultBuilder([])
                .ConfigureServices((context, services) =>
                {
                    // Configure Azure OpenAI services
                    string endpoint = GetRequiredEnvironmentVariable("AOAI_ENDPOINT");
                    string apiKey = GetRequiredEnvironmentVariable("AOAI_API_KEY");

                    // services.AddOpenAIChatCompletion("gpt-5-nano", apiKey);
                    services.AddAzureOpenAIChatCompletion("gpt-5-nano", endpoint, apiKey);
                    services.AddSingleton((s) => new Kernel(s));
                    services.AddSingleton<InProcessRuntime>();
                });

            IHost host = builder.Build();
            return host.Services;
        }

        private async Task<InProcessRuntime> GetRuntimeAsync()
        {
            if (this._runtime == null)
            {
                this._runtime = this._serviceProvider.GetRequiredService<InProcessRuntime>();
                await this._runtime.StartAsync();
            }

            return this._runtime;
        }

        public async Task<SequentialOrchestration> CreateOrchestration(IEnumerable<AgentDefinition> agentDefinitions)
        {
            List<ChatCompletionAgent> agents = new List<ChatCompletionAgent>();

            foreach (AgentDefinition definition in agentDefinitions)
            {
                ChatCompletionAgent agent = this.CreateChatCompletionAgent(definition);
                agents.Add(agent);
            }

            return new SequentialOrchestration(agents.ToArray());
        }

        // Helper method to execute orchestration with streaming support
        public async IAsyncEnumerable<string> ExecuteOrchestrationStreamingAsync(
            SequentialOrchestration orchestration,
            string input,
            ChatHistory history,
            IDictionary<string, string> arguments)
        {
            InProcessRuntime runtime = await this.GetRuntimeAsync();
            string output = string.Empty;

            try
            {
                OrchestrationResult<string> result = await orchestration.InvokeAsync(input, runtime);
                output = await result.GetValueAsync(TimeSpan.FromSeconds(120));
            }
            catch (Exception ex)
            {
                output = $"Orchestration failed: {ex.Message}";
            }
            finally
            {
                await runtime.RunUntilIdleAsync();
            }

            // Return the final output as a single chunk for now
            // TODO: Implement proper streaming when SequentialOrchestration supports it
            yield return output;
        }

        // Helper method to get agent names from orchestration
        public static IEnumerable<string> GetOrchestrationAgentNames(SequentialOrchestration orchestration)
        {
            // TODO: Implement when SequentialOrchestration provides access to agent names
            // For now, return a placeholder
            return new[] { "Sequential Orchestration" };
        }

        private ChatCompletionAgent CreateChatCompletionAgent(AgentDefinition definition)
        {
            PromptTemplateConfig templateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(
                File.ReadAllText(definition.PromptyPath));

            string instructions = File.ReadAllText(definition.InstructionsPath);

            return new ChatCompletionAgent(templateConfig, new LiquidPromptTemplateFactory())
            {
                Name = definition.Name,
                Instructions = instructions,
                Description = definition.Description,
                Kernel = this._serviceProvider.GetRequiredService<Kernel>()
            };
        }

        private static ChatCompletionAgent CreateChatCompletionAgentStatic(AgentDefinition definition)
        {
            PromptTemplateConfig templateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(
                File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), definition.PromptyPath)));

            string instructions = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), definition.InstructionsPath));
            // string endpoint = GetRequiredEnvironmentVariable("AOAI_ENDPOINT");
            string endpoint = GetRequiredEnvironmentVariable("AOAI_ENDPOINT");
            string apiKey = GetRequiredEnvironmentVariable("AOAI_API_KEY");

            Kernel kernel = Kernel.CreateBuilder()
                // .AddOpenAIChatCompletion("gpt-5-nano", apiKey)
                .AddAzureOpenAIChatCompletion("gpt-5-nano", endpoint, apiKey)
                .Build();

            return new ChatCompletionAgent(templateConfig, new LiquidPromptTemplateFactory())
            {
                Name = definition.Name,
                Instructions = instructions,
                Description = definition.Description,
                Kernel = kernel
            };
        }

        private static ChatAgent CreateAgentFromDefinition(AgentDefinition definition)
        {
            ChatCompletionAgent agent = CreateChatCompletionAgentStatic(definition);
            return new ChatAgent(agent);
        }

        private static string GetRequiredEnvironmentVariable(string key)
        {
            return Environment.GetEnvironmentVariable(key)
                ?? throw new InvalidOperationException($"{key} environment variable is not set.");
        }

        public static ChatAgent CreateAgent(string name, string description, string instructionsPath, string promptyPath)
        {
            AgentDefinition definition = new AgentDefinition
            {
                Name = name,
                Description = description,
                InstructionsPath = instructionsPath,
                PromptyPath = promptyPath
            };

            return CreateAgentFromDefinition(definition);
        }

        public static IEnumerable<ChatAgent> LoadAgentsFromConfig()
        {
            string config = File.ReadAllText("agents.config.json");
            AgentDefinition[]? agentDefinitions = JsonSerializer.Deserialize<AgentDefinition[]>(config);

            return agentDefinitions?.Select(CreateAgentFromDefinition) ?? Enumerable.Empty<ChatAgent>();
        }

        public void AddAgent(ChatAgent agent)
        {
            this._agents.Add(agent);
        }

        public async Task StartRuntimeAsync()
        {
            await this.GetRuntimeAsync();
        }

        public async Task StopRuntimeAsync()
        {
            if (this._runtime != null)
            {
                await this._runtime.StopAsync();
                this._runtime = null;
            }
        }

        public async Task RunUntilIdleAsync()
        {
            if (this._runtime != null)
            {
                await this._runtime.RunUntilIdleAsync();
            }
        }

        public InProcessRuntime? GetRuntime()
        {
            return this._runtime;
        }
    }
}
