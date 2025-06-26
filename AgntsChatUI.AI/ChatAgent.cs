namespace AgntsChatUI.AI
{
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.ChatCompletion;

    public class ChatAgent(ChatCompletionAgent innerAgent)
    {
        public async IAsyncEnumerable<string> InvokeStreamingAsyncInvokeAsync(
            string input,
            ChatHistory history,
            IDictionary<string, string> arguments)
        {
            if (innerAgent == null)
            {
                throw new InvalidOperationException("Agent is not initialized.");
            }

            ChatHistoryAgentThread thread = new ChatHistoryAgentThread(history);

            KernelArguments kernelArgs = new KernelArguments()
            {
                ["message"] = input
            };

            if (arguments != null)
            {
                foreach (KeyValuePair<string, string> arg in arguments)
                {
                    kernelArgs[arg.Key] = arg.Value;
                }
            }

            IAsyncEnumerable<AgentResponseItem<StreamingChatMessageContent>> result = innerAgent.InvokeStreamingAsync(
                input,
                thread,
                new AgentInvokeOptions()
                {
                    KernelArguments = kernelArgs
                });

            await foreach (AgentResponseItem<StreamingChatMessageContent> item in result)
            {
                yield return item.Message.Content ?? string.Empty;
            }
        }
    }
}
