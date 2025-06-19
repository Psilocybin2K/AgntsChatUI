namespace AgntsChatUI.Models
{
    using System;

    using CommunityToolkit.Mvvm.ComponentModel;

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