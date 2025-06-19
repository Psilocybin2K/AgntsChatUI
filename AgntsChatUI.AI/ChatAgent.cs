namespace AgntsChatUI.AI
{
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.ChatCompletion;

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
}
