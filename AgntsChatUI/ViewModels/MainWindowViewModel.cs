namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    using Avalonia.Controls;
    using Avalonia.Platform.Storage;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public ChatViewModel ChatViewModel { get; } = new();
    }

    public partial class ChatViewModel : ViewModelBase
    {
        private readonly IDocumentService _documentService;

        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private ContextDocument? selectedDocument;

        [ObservableProperty]
        private bool isTyping;

        public ObservableCollection<ContextDocument> Documents { get; } = [];
        public ObservableCollection<Message> Messages { get; } = [];

        public event Action? ScrollToBottomRequested;

        public ChatViewModel()
        {
            this._documentService = new DocumentService();
            this.InitializeMessages();
            _ = this.LoadDocumentsAsync();
        }

        [RelayCommand]
        private async Task UploadDocument()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);

            if (topLevel == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select ContextDocument",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    FilePickerFileTypes.All,
                    new FilePickerFileType("Documents") { Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt" } },
                    new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp" } },
                    new FilePickerFileType("Spreadsheets") { Patterns = new[] { "*.xls", "*.xlsx" } }
                }
            });

            if (files.Count > 0)
            {
                IStorageFile file = files[0];
                ContextDocument? document = await this._documentService.SaveDocumentAsync(file.Path.LocalPath);
                if (document != null)
                {
                    this.Documents.Insert(0, document);
                }
            }
        }

        [RelayCommand]
        private void DeleteDocument(ContextDocument document)
        {
            this._documentService.DeleteDocument(document.Id);
            this.Documents.Remove(document);
        }

        [RelayCommand]
        private void SelectDocument(ContextDocument document)
        {
            this.SelectedDocument = document;
        }

        private async Task LoadDocumentsAsync()
        {
            System.Collections.Generic.IEnumerable<ContextDocument> documents = await this._documentService.LoadDocumentsAsync();
            this.Documents.Clear();
            foreach (ContextDocument doc in documents)
            {
                this.Documents.Add(doc);
            }

            this.SelectedDocument = this.Documents.FirstOrDefault();
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
            this.ScrollToBottomRequested?.Invoke();

            // Show typing indicator
            this.IsTyping = true;

            // Scroll to bottom to show typing indicator
            this.ScrollToBottomRequested?.Invoke();

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
            this.ScrollToBottomRequested?.Invoke();
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

    public record Contact(string Avatar, string Name, string LastMessage, bool IsOnline, string AvatarColor);

    public partial class Message : ObservableObject
    {
        public string Id { get; }
        public string Avatar { get; }

        [ObservableProperty]
        private string content;

        public DateTime Time { get; }
        public bool IsSent { get; }
        public bool HasFile { get; }
        public string FileName { get; }
        public string FileSize { get; }

        [ObservableProperty]
        private bool isEditing;

        public Message(string avatar, string content, DateTime time, bool isSent, bool hasFile = false, string fileName = "", string fileSize = "")
        {
            this.Id = Guid.NewGuid().ToString();
            this.Avatar = avatar;
            this.Content = content;
            this.Time = time;
            this.IsSent = isSent;
            this.HasFile = hasFile;
            this.FileName = fileName;
            this.FileSize = fileSize;
        }
    }
}