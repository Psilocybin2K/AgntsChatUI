namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class ChatViewModel : ViewModelBase
    {
        private readonly DocumentManagementViewModel _documentManagementViewModel;

        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private bool isTyping;

        public ObservableCollection<MessageResult> Messages { get; } = [];

        public event Action? ScrollToBottomRequested;

        public ChatViewModel() : this(null)
        {
        }

        public ChatViewModel(DocumentManagementViewModel? documentManagementViewModel)
        {
            this._documentManagementViewModel = documentManagementViewModel ?? new DocumentManagementViewModel();
            this.InitializeMessages();
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(this.MessageText))
            {
                return;
            }

            string originalMessage = this.MessageText;
            string messageWithContext = await this.BuildMessageWithDocumentContext(originalMessage);

            MessageResult userMessage = new MessageResult(
                "ME",
                originalMessage, // Display only the user's original message
                DateTime.Now,
                true);

            this.Messages.Add(userMessage);
            this.MessageText = string.Empty;

            // Scroll to bottom after adding user message
            ScrollToBottomRequested?.Invoke();

            // Show typing indicator
            this.IsTyping = true;

            // Scroll to bottom to show typing indicator
            ScrollToBottomRequested?.Invoke();

            // Simulate response delay
            await Task.Delay(2000);

            // Generate response using message with document context
            string response = this.GenerateResponse(messageWithContext);
            MessageResult botMessage = new MessageResult(
                "JD",
                response,
                DateTime.Now,
                false);

            this.Messages.Add(botMessage);
            this.IsTyping = false;

            // Scroll to bottom after adding bot response
            ScrollToBottomRequested?.Invoke();
        }

        private async Task<string> BuildMessageWithDocumentContext(string userMessage)
        {
            System.Collections.Generic.List<ContextDocument> includedDocuments = this._documentManagementViewModel.Documents
                .Where(doc => doc.IsIncludedInChat)
                .ToList();

            if (!includedDocuments.Any())
            {
                return userMessage;
            }

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(userMessage);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("--- DOCUMENT CONTEXT ---");

            foreach (ContextDocument? document in includedDocuments)
            {
                messageBuilder.AppendLine($"--- {document.Title} ---");

                try
                {
                    string documentText = await this.ReadDocumentText(document.FilePath);
                    messageBuilder.AppendLine(documentText);
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"[Error reading document: {ex.Message}]");
                }

                messageBuilder.AppendLine();
            }

            return messageBuilder.ToString();
        }

        private async Task<string> ReadDocumentText(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "[Document file not found]";
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => await File.ReadAllTextAsync(filePath),
                ".pdf" => "[PDF content extraction not implemented - placeholder text]",
                ".doc" or ".docx" => "[Word document content extraction not implemented - placeholder text]",
                ".xls" or ".xlsx" => "[Excel document content extraction not implemented - placeholder text]",
                _ => $"[Content extraction not supported for {extension} files]"
            };
        }

        [RelayCommand]
        private void EditMessage(MessageResult message)
        {
            message.IsEditing = true;
        }

        [RelayCommand]
        private void SaveEdit(MessageResult message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit(MessageResult message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void DeleteMessage(MessageResult message)
        {
            this.Messages.Remove(message);
        }

        private string GenerateResponse(string messageWithContext)
        {
            // Check if message contains document context
            bool hasDocumentContext = messageWithContext.Contains("--- DOCUMENT CONTEXT ---");

            string[] contextAwareResponses = new[]
            {
                "Based on the documents you've shared, I can see that...",
                "Looking at your document content, it appears that...",
                "From the information in your documents, I understand...",
                "The documents provide good context. Let me help with...",
                "I've reviewed the document content you included...",
                "Based on the document context provided..."
            };

            string[] generalResponses = new[]
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
            string[] responsesToUse = hasDocumentContext ? contextAwareResponses : generalResponses;
            return responsesToUse[random.Next(responsesToUse.Length)];
        }

        private void InitializeMessages()
        {
            this.Messages.Add(new MessageResult("JD", "Hey! How's the project coming along?", DateTime.Now.AddMinutes(-30), false));
            this.Messages.Add(new MessageResult("ME", "Going well! Just finished the main features. Should be ready for review tomorrow.", DateTime.Now.AddMinutes(-28), true));
            this.Messages.Add(new MessageResult("JD", "That sounds great! Let's schedule a demo session.", DateTime.Now.AddMinutes(-25), false));
            this.Messages.Add(new MessageResult("JD", "Here's the design mockup we discussed", DateTime.Now.AddMinutes(-24), false, hasFile: true, fileName: "design-mockup-v2.pdf", fileSize: "2.4 MB"));
            this.Messages.Add(new MessageResult("ME", "Perfect! How about 3 PM tomorrow?", DateTime.Now.AddMinutes(-24), true));
        }
    }

}