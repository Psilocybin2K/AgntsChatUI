namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;

    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    using Avalonia.Controls;
    using Avalonia.Platform.Storage;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    public partial class DocumentManagementViewModel : ViewModelBase
    {
        private readonly IDocumentService _documentService;

        [ObservableProperty]
        private ContextDocument? selectedDocument;

        [ObservableProperty]
        private string uploadTitle = string.Empty;

        [ObservableProperty]
        private bool isUploadDialogOpen;

        public ObservableCollection<ContextDocument> Documents { get; } = [];

        public event Action<ContextDocument>? DocumentSelected;

        public DocumentManagementViewModel() : this(new DocumentService())
        {
        }

        public DocumentManagementViewModel(IDocumentService documentService)
        {
            this._documentService = documentService;
            _ = this.LoadDocumentsAsync();
        }

        [RelayCommand]
        private async Task UploadDocument()
        {
            TopLevel? topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow : null);

            if (topLevel == null)
            {
                return;
            }

            System.Collections.Generic.IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Document",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    FilePickerFileTypes.All,
                    new FilePickerFileType("Documents") { Patterns = new[] { "*.pdf", "*.doc", "*.docx", "*.txt" } },
                    new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp" } },
                    new FilePickerFileType("Spreadsheets") { Patterns = new[] { "*.xls", "*.xlsx" } }
                }
            });

            if (files.Count > 0)
            {
                IStorageFile file = files[0];
                string defaultTitle = System.IO.Path.GetFileNameWithoutExtension(file.Name);
                this.UploadTitle = defaultTitle;
                this.IsUploadDialogOpen = true;

                // The actual upload will happen when user confirms in dialog
                this._pendingFilePath = file.Path.LocalPath;
            }
        }

        private string _pendingFilePath = string.Empty;

        [RelayCommand]
        private async Task ConfirmUpload()
        {
            if (string.IsNullOrWhiteSpace(this._pendingFilePath))
            {
                return;
            }

            string title = string.IsNullOrWhiteSpace(this.UploadTitle) ? "Unknown Document" : this.UploadTitle.Trim();
            ContextDocument? document = await this._documentService.SaveDocumentAsync(this._pendingFilePath, title);

            if (document != null)
            {
                this.Documents.Insert(0, document);
                this.SelectDocument(document);
            }

            this.IsUploadDialogOpen = false;
            this.UploadTitle = string.Empty;
            this._pendingFilePath = string.Empty;
        }

        [RelayCommand]
        private void CancelUpload()
        {
            this.IsUploadDialogOpen = false;
            this.UploadTitle = string.Empty;
            this._pendingFilePath = string.Empty;
        }

        [RelayCommand]
        private void EditDocumentTitle(ContextDocument document)
        {
            document.IsEditingTitle = true;
        }

        [RelayCommand]
        private async Task SaveDocumentTitle(ContextDocument document)
        {
            if (!string.IsNullOrWhiteSpace(document.Title))
            {
                await this._documentService.UpdateDocumentTitleAsync(document.Id, document.Title);
            }
            document.IsEditingTitle = false;
        }

        [RelayCommand]
        private void CancelEditTitle(ContextDocument document)
        {
            // Reload the document to revert changes
            _ = Task.Run(async () =>
            {
                System.Collections.Generic.IEnumerable<ContextDocument> documents = await this._documentService.LoadDocumentsAsync();
                ContextDocument? originalDoc = documents.FirstOrDefault(d => d.Id == document.Id);
                if (originalDoc != null)
                {
                    document.Title = originalDoc.Title;
                }
                document.IsEditingTitle = false;
            });
        }

        [RelayCommand]
        private void DeleteDocument(ContextDocument document)
        {
            this._documentService.DeleteDocument(document.Id);
            this.Documents.Remove(document);

            // If the deleted document was selected, select the first available document
            if (this.SelectedDocument?.Id == document.Id)
            {
                this.SelectedDocument = this.Documents.FirstOrDefault();
                if (this.SelectedDocument != null)
                {
                    DocumentSelected?.Invoke(this.SelectedDocument);
                }
            }
        }

        [RelayCommand]
        private void SelectDocument(ContextDocument document)
        {
            this.SelectedDocument = document;
            DocumentSelected?.Invoke(document);
        }

        private async Task LoadDocumentsAsync()
        {
            System.Collections.Generic.IEnumerable<ContextDocument> documents = await this._documentService.LoadDocumentsAsync();
            this.Documents.Clear();
            foreach (ContextDocument doc in documents)
            {
                this.Documents.Add(doc);
            }

            this.SelectedDocument = this.Documents.FirstOrDefault();
            if (this.SelectedDocument != null)
            {
                DocumentSelected?.Invoke(this.SelectedDocument);
            }
        }
    }
}