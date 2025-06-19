namespace AgntsChatUI.Models
{
    using System;

    using CommunityToolkit.Mvvm.ComponentModel;

    public partial class ContextDocument : ObservableObject
    {
        public string Id { get; }
        public string FileName { get; }
        public string Size { get; }
        public DateTime DateAdded { get; }
        public string FilePath { get; }
        public DocumentType FileType { get; }

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool isEditingTitle;

        public ContextDocument(string id, string fileName, string size, DateTime dateAdded, string filePath, DocumentType fileType, string? customTitle = null)
        {
            this.Id = id;
            this.FileName = fileName;
            this.Size = size;
            this.DateAdded = dateAdded;
            this.FilePath = filePath;
            this.FileType = fileType;
            this.Title = customTitle ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
        }

        public string FileTypeIcon => this.FileType switch
        {
            DocumentType.Pdf => "📄",
            DocumentType.Word => "📝",
            DocumentType.Excel => "📊",
            DocumentType.PowerPoint => "📋",
            DocumentType.Text => "📄",
            DocumentType.Image => "🖼️",
            _ => "📁"
        };

        public string FileTypeColor => this.FileType switch
        {
            DocumentType.Pdf => "#ea4335",
            DocumentType.Word => "#4285f4",
            DocumentType.Excel => "#34a853",
            DocumentType.PowerPoint => "#fbbc05",
            DocumentType.Text => "#5f6368",
            DocumentType.Image => "#9c27b0",
            _ => "#757575"
        };
    }
}