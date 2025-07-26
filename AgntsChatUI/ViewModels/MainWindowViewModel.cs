namespace AgntsChatUI.ViewModels
{
    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public DocumentManagementViewModel DocumentManagementViewModel { get; }
        public ChatViewModel ChatViewModel { get; }
        public AgentManagementViewModel AgentManagementViewModel { get; }

        public MainWindowViewModel(IAgentService agentService, IFileTemplateService fileTemplateService, AgentManagementViewModel agentManagementViewModel)
        {
            this.DocumentManagementViewModel = new DocumentManagementViewModel();
            this.ChatViewModel = new ChatViewModel(this.DocumentManagementViewModel, agentService);
            this.AgentManagementViewModel = agentManagementViewModel;

            // Subscribe to document selection changes
            this.DocumentManagementViewModel.DocumentSelected += this.OnDocumentSelected;

            // Subscribe to agent changes to refresh chat agents
            this.AgentManagementViewModel.AgentChanged += this.OnAgentChanged;
        }

        private void OnDocumentSelected(ContextDocument document)
        {
            // Document selection handling - could be used for additional chat context updates
        }

        private async void OnAgentChanged()
        {
            // Refresh the chat agents when agents are modified
            await this.ChatViewModel.LoadAgentsAsync();
        }
    }
}