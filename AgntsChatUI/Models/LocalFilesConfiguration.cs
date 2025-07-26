namespace AgntsChatUI.Models
{
    // Configuration model for single file data source
    public class LocalFilesConfiguration
    {
        public string FilePath { get; set; } = "";
        public string[] SupportedExtensions { get; set; } = new[] { ".txt", ".md", ".json", ".xml", ".csv", ".pdf", ".doc", ".docx" };
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    }
}