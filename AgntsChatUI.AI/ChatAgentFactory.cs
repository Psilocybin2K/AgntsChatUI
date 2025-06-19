namespace AgntsChatUI.AI
{
    using System.Text.Json;

    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.PromptTemplates.Liquid;
    using Microsoft.SemanticKernel.Prompty;

    public class AgentDefinition
    {
        public string Description { get; set; }
        public string InstructionsPath { get; set; }
        public string Name { get; set; }
        public string PromptyPath { get; set; }
    }

    public class ChatAgentFactory
    {
        private readonly IList<ChatAgent> _agents;

        public ChatAgentFactory()
        {
            this._agents = [.. LoadAgentsFromConfig()];
        }

        public static ChatAgent CreateAgent(string name,
            string description,
            string instructionsPath,
            string promptyPath)
        {
            PromptTemplateConfig templateConfig =
                KernelFunctionPrompty.ToPromptTemplateConfig(File.ReadAllText(promptyPath));

            string instructions = File.ReadAllText(instructionsPath);
            string endpoint = Environment.GetEnvironmentVariable("AOAI_ENDPOINT") ??
                              throw new InvalidOperationException("AOAI_ENDPOINT environment variable is not set.");
            string apiKey = Environment.GetEnvironmentVariable("AOAI_API_KEY") ??
                            throw new InvalidOperationException("AOAI_API_KEY environment variable is not set.");

            ChatCompletionAgent agent = new ChatCompletionAgent(templateConfig, new LiquidPromptTemplateFactory())
            {
                Name = name,
                Instructions = instructions,
                Description = description,
                Kernel = Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion("gpt-4.1-nano", endpoint, apiKey)
                    .Build()
            };

            ChatAgent chatAgent = new ChatAgent(agent);

            return chatAgent;
        }

        public static IEnumerable<ChatAgent> LoadAgentsFromConfig()
        {
            string config = File.ReadAllText("agents.config.json");
            AgentDefinition[]? agentDefinitions = JsonSerializer.Deserialize<AgentDefinition[]>(config);

            IEnumerable<ChatAgent> agents = agentDefinitions.Select(a =>
                CreateAgent(a.Name, a.Description, a.InstructionsPath, a.PromptyPath));

            return agents;
        }

        public void AddAgent(ChatAgent agent)
        {
            this._agents.Add(agent);
        }
    }
}
