namespace AgntsChatUI.ViewModels
{
    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    public partial class MainWindowViewModel : ViewModelBase
    {
        public DataSourceManagementViewModel DataSourceManagementViewModel { get; }
        public ChatViewModel ChatViewModel { get; }
        public AgentManagementViewModel AgentManagementViewModel { get; }

        private bool _isCommandPaletteVisible;
        public bool IsCommandPaletteVisible
        {
            get => this._isCommandPaletteVisible;
            set => this.SetProperty(ref this._isCommandPaletteVisible, value);
        }

        public MainWindowViewModel(IAgentService agentService, IFileTemplateService fileTemplateService, 
            AgentManagementViewModel agentManagementViewModel, DataSourceManagementViewModel dataSourceManagementViewModel,
            IDataSourceManager dataSourceManager)
        {
            DataSourceManagementViewModel = dataSourceManagementViewModel;
            
            ChatViewModel = new ChatViewModel(agentService, dataSourceManager);
            
            AgentManagementViewModel = agentManagementViewModel;
            
            AgentManagementViewModel.AgentChanged += OnAgentChanged;
        }
        
        private async void OnAgentChanged()
        {
            // Refresh the chat agents when agents are modified
            await this.ChatViewModel.LoadAgentsAsync();
        }
    }
}