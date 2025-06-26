namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    public interface IDocumentService
    {
        Task<IEnumerable<ContextDocument>> LoadDocumentsAsync();
        Task<ContextDocument?> SaveDocumentAsync(string filePath, string? customTitle = null);
        Task UpdateDocumentTitleAsync(string documentId, string newTitle);
        void DeleteDocument(string documentId);
        string GetDocumentPath(string fileName);
    }

    public class DocumentService : IDocumentService
    {
        private readonly string _documentsPath;
        private readonly string _metadataFile;

        public DocumentService()
        {
            this._documentsPath = Path.Combine(Environment.CurrentDirectory, "Documents");
            this._metadataFile = Path.Combine(this._documentsPath, "metadata.json");
            this.EnsureDirectoryExists();
        }

        public async Task<IEnumerable<ContextDocument>> LoadDocumentsAsync()
        {
            if (!Directory.Exists(this._documentsPath))
            {
                return Enumerable.Empty<ContextDocument>();
            }

            Dictionary<string, DocumentMetadata> metadata = await this.LoadMetadataAsync();
            List<ContextDocument> documents = new List<ContextDocument>();
            string[] files = Directory.GetFiles(this._documentsPath).Where(f => !f.EndsWith("metadata.json")).ToArray();

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                string documentId = Path.GetFileNameWithoutExtension(file);

                string? customTitle = null;
                if (metadata.TryGetValue(documentId, out DocumentMetadata? meta))
                {
                    customTitle = meta.Title;
                }

                ContextDocument document = new ContextDocument(
                    documentId,
                    Path.GetFileName(file),
                    FormatFileSize(fileInfo.Length),
                    fileInfo.CreationTime,
                    file,
                    DocumentTypeHelper.GetFileType(fileInfo.Extension),
                    customTitle
                );
                documents.Add(document);
            }

            return documents.OrderByDescending(d => d.DateAdded);
        }

        public async Task<ContextDocument?> SaveDocumentAsync(string sourceFilePath, string? customTitle = null)
        {
            if (!File.Exists(sourceFilePath))
            {
                return null;
            }

            string fileName = Path.GetFileName(sourceFilePath);
            string destinationPath = Path.Combine(this._documentsPath, fileName);

            // Handle duplicate names
            int counter = 1;
            string originalDestination = destinationPath;
            while (File.Exists(destinationPath))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(originalDestination);
                string extension = Path.GetExtension(originalDestination);
                destinationPath = Path.Combine(this._documentsPath, $"{nameWithoutExt}_{counter}{extension}");
                counter++;
            }

            File.Copy(sourceFilePath, destinationPath);
            FileInfo fileInfo = new FileInfo(destinationPath);
            string documentId = Path.GetFileNameWithoutExtension(destinationPath);

            // Save custom title if provided
            if (!string.IsNullOrWhiteSpace(customTitle))
            {
                await this.UpdateDocumentTitleAsync(documentId, customTitle);
            }

            return new ContextDocument(
                documentId,
                Path.GetFileName(destinationPath),
                FormatFileSize(fileInfo.Length),
                fileInfo.CreationTime,
                destinationPath,
                DocumentTypeHelper.GetFileType(fileInfo.Extension),
                customTitle
            );
        }

        public async Task UpdateDocumentTitleAsync(string documentId, string newTitle)
        {
            Dictionary<string, DocumentMetadata> metadata = await this.LoadMetadataAsync();

            metadata[documentId] = new DocumentMetadata { Title = newTitle };

            await this.SaveMetadataAsync(metadata);
        }

        public void DeleteDocument(string documentId)
        {
            // Delete the file
            string[] files = Directory.GetFiles(this._documentsPath);
            string? targetFile = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == documentId);

            if (targetFile != null && File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }

            // Remove from metadata
            _ = Task.Run(async () =>
            {
                Dictionary<string, DocumentMetadata> metadata = await this.LoadMetadataAsync();
                metadata.Remove(documentId);
                await this.SaveMetadataAsync(metadata);
            });
        }

        public string GetDocumentPath(string fileName)
        {
            return Path.Combine(this._documentsPath, fileName);
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(this._documentsPath))
            {
                Directory.CreateDirectory(this._documentsPath);
            }
        }

        private async Task<Dictionary<string, DocumentMetadata>> LoadMetadataAsync()
        {
            if (!File.Exists(this._metadataFile))
            {
                return new Dictionary<string, DocumentMetadata>();
            }

            try
            {
                string json = await File.ReadAllTextAsync(this._metadataFile);
                return JsonSerializer.Deserialize<Dictionary<string, DocumentMetadata>>(json) ?? new Dictionary<string, DocumentMetadata>();
            }
            catch
            {
                return new Dictionary<string, DocumentMetadata>();
            }
        }

        private async Task SaveMetadataAsync(Dictionary<string, DocumentMetadata> metadata)
        {
            try
            {
                string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(this._metadataFile, json);
            }
            catch
            {
                // Handle save errors gracefully
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class DocumentMetadata
    {
        public string Title { get; set; } = string.Empty;
    }
}