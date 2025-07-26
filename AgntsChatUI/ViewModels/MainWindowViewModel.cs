namespace AgntsChatUI.ViewModels
{
    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public DocumentManagementViewModel DocumentManagementViewModel { get; }
        public ChatViewModel ChatViewModel { get; }

        public MainWindowViewModel(IAgentService agentService)
        {
            this.DocumentManagementViewModel = new DocumentManagementViewModel();
            this.ChatViewModel = new ChatViewModel(this.DocumentManagementViewModel, agentService);

            // Subscribe to document selection changes
            this.DocumentManagementViewModel.DocumentSelected += this.OnDocumentSelected;
        }

        private void OnDocumentSelected(ContextDocument document)
        {
            // Document selection handling - could be used for additional chat context updates
        }
    }
}