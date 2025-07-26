namespace AgntsChatUI.Models
{
    using System;

    public class ChatResponseLog(string response, string agent, DateTime timestamp)
    {
        public string Response { get; } = response;
        public string Agent { get; } = agent;
        public DateTime Timestamp { get; } = timestamp;
    }
}