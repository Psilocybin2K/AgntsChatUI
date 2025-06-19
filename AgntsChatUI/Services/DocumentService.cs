namespace AgntsChatUI.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;

    public interface IDocumentService
    {
        Task<IEnumerable<ContextDocument>> LoadDocumentsAsync();
        Task<ContextDocument?> SaveDocumentAsync(string filePath);
        void DeleteDocument(string documentId);
        string GetDocumentPath(string fileName);
    }

    public class DocumentService : IDocumentService
    {
        private readonly string _documentsPath;

        public DocumentService()
        {
            this._documentsPath = Path.Combine(Environment.CurrentDirectory, "Documents");
            this.EnsureDirectoryExists();
        }

        public async Task<IEnumerable<ContextDocument>> LoadDocumentsAsync()
        {
            if (!Directory.Exists(this._documentsPath))
            {
                return Enumerable.Empty<ContextDocument>();
            }

            List<ContextDocument> documents = new List<ContextDocument>();
            string[] files = Directory.GetFiles(this._documentsPath);

            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                ContextDocument document = new ContextDocument(
                    Path.GetFileNameWithoutExtension(file),
                    Path.GetFileName(file),
                    FormatFileSize(fileInfo.Length),
                    fileInfo.CreationTime,
                    file,
                    GetFileType(fileInfo.Extension)
                );
                documents.Add(document);
            }

            return documents.OrderByDescending(d => d.DateAdded);
        }

        public async Task<ContextDocument?> SaveDocumentAsync(string sourceFilePath)
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

            return new ContextDocument(
                Path.GetFileNameWithoutExtension(destinationPath),
                Path.GetFileName(destinationPath),
                FormatFileSize(fileInfo.Length),
                fileInfo.CreationTime,
                destinationPath,
                GetFileType(fileInfo.Extension)
            );
        }

        public void DeleteDocument(string documentId)
        {
            string[] files = Directory.GetFiles(this._documentsPath);
            string? targetFile = files.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == documentId);

            if (targetFile != null && File.Exists(targetFile))
            {
                File.Delete(targetFile);
            }
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

        private static DocumentType GetFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => DocumentType.Pdf,
                ".doc" or ".docx" => DocumentType.Word,
                ".xls" or ".xlsx" => DocumentType.Excel,
                ".ppt" or ".pptx" => DocumentType.PowerPoint,
                ".txt" => DocumentType.Text,
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => DocumentType.Image,
                _ => DocumentType.Other
            };
        }
    }
}