namespace AgntsChatUI.ViewModels
{
    using AgntsChatUI.Models;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public DocumentManagementViewModel DocumentManagementViewModel { get; }
        public ChatViewModel ChatViewModel { get; }

        public MainWindowViewModel()
        {
            this.DocumentManagementViewModel = new DocumentManagementViewModel();
            this.ChatViewModel = new ChatViewModel(this.DocumentManagementViewModel);

            // Subscribe to document selection changes
            this.DocumentManagementViewModel.DocumentSelected += this.OnDocumentSelected;
        }

        private void OnDocumentSelected(ContextDocument document)
        {
            // Document selection handling - could be used for additional chat context updates
        }
    }
}