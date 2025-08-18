namespace AgntsChatUI.ViewModels
{
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

		private bool _isResponsiveMode = false;
		public bool IsResponsiveMode
		{
			get => this._isResponsiveMode;
			set => this.SetProperty(ref this._isResponsiveMode, value);
		}

        public MainWindowViewModel(IAgentService agentService, IFileTemplateService fileTemplateService,
            AgentManagementViewModel agentManagementViewModel, DataSourceManagementViewModel dataSourceManagementViewModel,
            IDataSourceManager dataSourceManager)
        {
            this.DataSourceManagementViewModel = dataSourceManagementViewModel;

            this.ChatViewModel = new ChatViewModel(agentService, dataSourceManager);

            this.AgentManagementViewModel = agentManagementViewModel;

            this.AgentManagementViewModel.AgentChanged += this.OnAgentChanged;
        }

        		private async void OnAgentChanged()
		{
			// Refresh the chat agents when agents are modified
			await this.ChatViewModel.LoadAgentsAsync();
		}

		/// <summary>
		/// Handles window resize events to adjust layout responsively
		/// </summary>
		/// <param name="width">New window width</param>
		/// <param name="height">New window height</param>
		public void OnWindowResized(double width, double height)
		{
			// Enable responsive mode for smaller screens
			this.IsResponsiveMode = width < 1200;
			
			// Auto-close agent panel on very small screens
			if (width < 800 && this.ChatViewModel.IsAgentPanelOpen)
			{
				this.ChatViewModel.IsAgentPanelOpen = false;
			}
		}

		/// <summary>
		/// Toggles responsive layout mode
		/// </summary>
		public void ToggleResponsiveMode()
		{
			this.IsResponsiveMode = !this.IsResponsiveMode;
		}
	}
}