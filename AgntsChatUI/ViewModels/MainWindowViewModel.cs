namespace AgntsChatUI.ViewModels
{
    using System;

    using AgntsChatUI.Models;

    using CommunityToolkit.Mvvm.ComponentModel;


    public partial class MainWindowViewModel : ViewModelBase
    {
        public DocumentManagementViewModel DocumentManagementViewModel { get; }
        public ChatViewModel ChatViewModel { get; }

        public MainWindowViewModel()
        {
            this.DocumentManagementViewModel = new DocumentManagementViewModel();
            this.ChatViewModel = new ChatViewModel();

            // Subscribe to document selection changes
            this.DocumentManagementViewModel.DocumentSelected += this.OnDocumentSelected;
        }

        private void OnDocumentSelected(ContextDocument document)
        {
            // This could be used to update the chat context or perform other actions
            // when a document is selected
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