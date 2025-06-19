namespace AgntsChatUI.Models
{
    using System;

    public record ContextDocument(
        string Id,
        string Name,
        string Size,
        DateTime DateAdded,
        string FilePath,
        DocumentType FileType
    )
    {
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