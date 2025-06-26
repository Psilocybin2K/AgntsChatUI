namespace AgntsChatUI.Models
{
    using System.Collections.Generic;

    public static class DocumentTypeHelper
    {
        private static readonly Dictionary<string, UploadedDocumentType> ExtensionMappings = new()
        {
            { ".pdf", UploadedDocumentType.Pdf },
            { ".doc", UploadedDocumentType.Word },
            { ".docx", UploadedDocumentType.Word },
            { ".xls", UploadedDocumentType.Excel },
            { ".xlsx", UploadedDocumentType.Excel },
            { ".ppt", UploadedDocumentType.PowerPoint },
            { ".pptx", UploadedDocumentType.PowerPoint },
            { ".txt", UploadedDocumentType.Text },
            { ".jpg", UploadedDocumentType.Image },
            { ".jpeg", UploadedDocumentType.Image },
            { ".png", UploadedDocumentType.Image },
            { ".gif", UploadedDocumentType.Image },
            { ".bmp", UploadedDocumentType.Image }
        };

        public static UploadedDocumentType GetFileType(string extension)
        {
            return ExtensionMappings.TryGetValue(extension.ToLowerInvariant(), out UploadedDocumentType fileType)
                ? fileType
                : UploadedDocumentType.Other;
        }
    }
}
