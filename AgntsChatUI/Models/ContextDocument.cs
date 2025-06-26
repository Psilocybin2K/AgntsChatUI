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
        public UploadedDocumentType FileType { get; }

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool isEditingTitle;

        [ObservableProperty]
        private bool isIncludedInChat;

        public ContextDocument(string id, string fileName, string size, DateTime dateAdded, string filePath, UploadedDocumentType fileType, string? customTitle = null, bool isIncludedInChat = false)
        {
            this.Id = id;
            this.FileName = fileName;
            this.Size = size;
            this.DateAdded = dateAdded;
            this.FilePath = filePath;
            this.FileType = fileType;
            this.Title = customTitle ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
            this.IsIncludedInChat = isIncludedInChat;
        }

        private static (string Icon, string Color) GetFileTypeDisplay(UploadedDocumentType fileType) => fileType switch
        {
            UploadedDocumentType.Pdf => ("📄", "#ea4335"),
            UploadedDocumentType.Word => ("📝", "#4285f4"),
            UploadedDocumentType.Excel => ("📊", "#34a853"),
            UploadedDocumentType.PowerPoint => ("📋", "#fbbc05"),
            UploadedDocumentType.Text => ("📄", "#5f6368"),
            UploadedDocumentType.Image => ("🖼️", "#9c27b0"),
            _ => ("📁", "#757575")
        };

        public string FileTypeIcon => GetFileTypeDisplay(this.FileType).Icon;
        public string FileTypeColor => GetFileTypeDisplay(this.FileType).Color;
    }
}