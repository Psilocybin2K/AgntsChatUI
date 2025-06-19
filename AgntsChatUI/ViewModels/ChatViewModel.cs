namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class ChatViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private bool isTyping;

        public ObservableCollection<Message> Messages { get; } = [];

        public event Action? ScrollToBottomRequested;

        public ChatViewModel()
        {
            this.InitializeMessages();
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(this.MessageText))
            {
                return;
            }

            Message userMessage = new Message(
                "ME",
                this.MessageText,
                DateTime.Now,
                true);

            this.Messages.Add(userMessage);
            string messageContent = this.MessageText;
            this.MessageText = string.Empty;

            // Scroll to bottom after adding user message
            ScrollToBottomRequested?.Invoke();

            // Show typing indicator
            this.IsTyping = true;

            // Scroll to bottom to show typing indicator
            ScrollToBottomRequested?.Invoke();

            // Simulate response delay
            await Task.Delay(2000);

            // Generate prototype response
            string response = this.GenerateResponse(messageContent);
            Message botMessage = new Message(
                "JD",
                response,
                DateTime.Now,
                false);

            this.Messages.Add(botMessage);
            this.IsTyping = false;

            // Scroll to bottom after adding bot response
            ScrollToBottomRequested?.Invoke();
        }

        [RelayCommand]
        private void EditMessage(Message message)
        {
            message.IsEditing = true;
        }

        [RelayCommand]
        private void SaveEdit(Message message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit(Message message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void DeleteMessage(Message message)
        {
            this.Messages.Remove(message);
        }

        private string GenerateResponse(string userMessage)
        {
            string[] responses = new[]
            {
                "That's interesting! Tell me more.",
                "I understand what you mean.",
                "Thanks for sharing that with me.",
                "That sounds great!",
                "I see your point.",
                "How do you feel about that?",
                "That makes sense.",
                "I appreciate you letting me know."
            };

            Random random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private void InitializeMessages()
        {
            this.Messages.Add(new Message("JD", "Hey! How's the project coming along?", DateTime.Now.AddMinutes(-30), false));
            this.Messages.Add(new Message("ME", "Going well! Just finished the main features. Should be ready for review tomorrow.", DateTime.Now.AddMinutes(-28), true));
            this.Messages.Add(new Message("JD", "That sounds great! Let's schedule a demo session.", DateTime.Now.AddMinutes(-25), false));
            this.Messages.Add(new Message("JD", "Here's the design mockup we discussed", DateTime.Now.AddMinutes(-24), false, hasFile: true, fileName: "design-mockup-v2.pdf", fileSize: "2.4 MB"));
            this.Messages.Add(new Message("ME", "Perfect! How about 3 PM tomorrow?", DateTime.Now.AddMinutes(-24), true));
        }
    }
}