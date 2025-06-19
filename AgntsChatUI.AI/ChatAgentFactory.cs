namespace AgntsChatUI.AI
{
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Microsoft.SemanticKernel.PromptTemplates.Liquid;
    using Microsoft.SemanticKernel.Prompty;

    public class ChatAgent(ChatCompletionAgent innerAgent)
    {
        public async IAsyncEnumerable<string> InvokeStreamingAsyncInvokeAsync(string input,
            ChatHistory history)
        {
            if (innerAgent == null)
            {
                throw new InvalidOperationException("Agent is not initialized.");
            }

            ChatHistoryAgentThread _thread = new ChatHistoryAgentThread(history);

            IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> result = innerAgent.InvokeStreamingAsync(input, _thread, new AgentInvokeOptions()
            {
                KernelArguments = new KernelArguments()
                {
                    ["message"] = input
                }
            });

            await foreach (AgentResponseItem<StreamingChatMessageContent> item in result)
            {
                yield return item.Message.Content ?? throw new InvalidOperationException("Message content is null.");
            }

        }
    }

    public class ChatAgentFactory
    {
        public static ChatCompletionAgent CreateAgent(string name,
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

            return agent;
        }
    }
}
