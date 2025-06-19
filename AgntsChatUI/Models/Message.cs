namespace AgntsChatUI.Models
{
    using System;

    public record Message(
        string Id,
        string Content,
        string SenderName,
        string SenderInitials,
        DateTime Timestamp,
        bool IsSent,
        string AvatarColor = "#4285f4"
    );

    public record ChatContact(
        string Id,
        string Name,
        string Initials,
        string LastMessage,
        DateTime LastMessageTime,
        bool IsOnline,
        string AvatarColor = "#4285f4"
    );

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

    public enum DocumentType
    {
        Pdf,
        Word,
        Excel,
        PowerPoint,
        Text,
        Image,
        Other
    }
}
