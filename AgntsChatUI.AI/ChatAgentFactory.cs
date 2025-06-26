namespace AgntsChatUI.AI
{
    using System.Text.Json;

    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.PromptTemplates.Liquid;
    using Microsoft.SemanticKernel.Prompty;

    public class ChatAgentFactory
    {
        private readonly IList<ChatAgent> _agents;

        public ChatAgentFactory()
        {
            this._agents = [.. LoadAgentsFromConfig()];
        }

        private static ChatAgent CreateAgentFromDefinition(AgentDefinition definition)
        {
            PromptTemplateConfig templateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(
                File.ReadAllText(definition.PromptyPath));

            string instructions = File.ReadAllText(definition.InstructionsPath);
            string endpoint = GetRequiredEnvironmentVariable("AOAI_ENDPOINT");
            string apiKey = GetRequiredEnvironmentVariable("AOAI_API_KEY");

            ChatCompletionAgent agent = new ChatCompletionAgent(templateConfig, new LiquidPromptTemplateFactory())
            {
                Name = definition.Name,
                Instructions = instructions,
                Description = definition.Description,
                Kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion("gpt-4.1-nano", endpoint, apiKey)
                    .Build()
            };

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
    }
}
