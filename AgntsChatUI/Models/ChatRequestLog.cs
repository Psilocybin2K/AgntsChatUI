namespace AgntsChatUI.Models
{
    using System;

    public class ChatRequestLog(string userMessage, string[] selectedAgents, DateTime timestamp)
    {
        public string UserMessage { get; } = userMessage;
        public string[] SelectedAgents { get; } = selectedAgents;
        public DateTime Timestamp { get; } = timestamp;
    }
}