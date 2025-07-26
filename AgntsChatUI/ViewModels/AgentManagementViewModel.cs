namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;
    using AgntsChatUI.Services;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>
    /// ViewModel for managing agents (create, edit, delete)
    /// </summary>
    public partial class AgentManagementViewModel : ViewModelBase
    {
        private readonly IAgentService _agentService;
        private readonly IFileTemplateService _fileTemplateService;

        [ObservableProperty]
        private ObservableCollection<AgentDefinition> agents = [];

        [ObservableProperty]
        private AgentDefinition? selectedAgent;

        [ObservableProperty]
        private bool isEditing;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string agentName = string.Empty;

        [ObservableProperty]
        private string agentDescription = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        public event Action? AgentChanged;

        public AgentManagementViewModel(IAgentService agentService, IFileTemplateService fileTemplateService)
        {
            this._agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            this._fileTemplateService = fileTemplateService ?? throw new ArgumentNullException(nameof(fileTemplateService));

            this.InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                // Initialize the agent service first
                await this._agentService.InitializeAsync();
                await this.LoadAgentsAsync();
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error initializing agent service: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads all agents from the service
        /// </summary>
        public async Task LoadAgentsAsync()
        {
            try
            {
                this.IsLoading = true;
                this.HasError = false;
                this.StatusMessage = "Loading agents...";

                System.Collections.Generic.IEnumerable<AgentDefinition> agentsList = await this._agentService.GetAllAgentsAsync();
                this.Agents.Clear();

                foreach (AgentDefinition agent in agentsList)
                {
                    this.Agents.Add(agent);
                }

                this.StatusMessage = $"Loaded {this.Agents.Count} agents";
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error loading agents: {ex.Message}";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Starts the process of adding a new agent
        /// </summary>
        [RelayCommand]
        private void AddAgent()
        {
            this.ClearForm();
            this.IsEditing = true;
            this.StatusMessage = "Adding new agent...";
            this.HasError = false;
        }

        /// <summary>
        /// Starts editing the selected agent
        /// </summary>
        [RelayCommand]
        private void EditAgent(AgentDefinition? agent = null)
        {
            AgentDefinition? agentToEdit = agent ?? this.SelectedAgent;
            if (agentToEdit == null)
            {
                this.StatusMessage = "Please select an agent to edit";
                this.HasError = true;
                return;
            }

            this.SelectedAgent = agentToEdit;
            this.AgentName = agentToEdit.Name;
            this.AgentDescription = agentToEdit.Description;
            this.IsEditing = true;
            this.StatusMessage = $"Editing agent: {agentToEdit.Name}";
            this.HasError = false;
        }

        /// <summary>
        /// Deletes the selected agent
        /// </summary>
        [RelayCommand]
        private async Task DeleteAgent(AgentDefinition? agent = null)
        {
            AgentDefinition? agentToDelete = agent ?? this.SelectedAgent;
            if (agentToDelete == null)
            {
                this.StatusMessage = "Please select an agent to delete";
                this.HasError = true;
                return;
            }

            try
            {
                this.IsLoading = true;
                this.HasError = false;
                this.StatusMessage = $"Deleting agent: {this.SelectedAgent.Name}...";

                bool success = await this._agentService.DeleteAgentAsync(this.SelectedAgent.Id ?? 0);

                if (success)
                {
                    this.Agents.Remove(this.SelectedAgent);
                    this.SelectedAgent = null;
                    this.StatusMessage = "Agent deleted successfully";
                    AgentChanged?.Invoke();
                }
                else
                {
                    this.StatusMessage = "Failed to delete agent";
                    this.HasError = true;
                }
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error deleting agent: {ex.Message}";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Saves the current agent (creates new or updates existing)
        /// </summary>
        [RelayCommand]
        private async Task SaveAgent()
        {
            if (string.IsNullOrWhiteSpace(this.AgentName) || string.IsNullOrWhiteSpace(this.AgentDescription))
            {
                this.StatusMessage = "Please provide both name and description";
                this.HasError = true;
                return;
            }

            try
            {
                this.IsLoading = true;
                this.HasError = false;
                this.StatusMessage = "Saving agent...";

                AgentDefinition agentToSave;

                if (this.SelectedAgent == null)
                {
                    // Creating new agent
                    string instructionPath = await this._fileTemplateService.CreateInstructionFileAsync(this.AgentName, this.AgentDescription);
                    string personaPath = await this._fileTemplateService.CreatePersonaFileAsync(this.AgentName, this.AgentDescription);

                    agentToSave = new AgentDefinition
                    {
                        Name = this.AgentName.Trim(),
                        Description = this.AgentDescription.Trim(),
                        InstructionsPath = instructionPath,
                        PromptyPath = personaPath
                    };

                    this.StatusMessage = "Creating new agent...";
                }
                else
                {
                    // Updating existing agent
                    agentToSave = new AgentDefinition
                    {
                        Id = this.SelectedAgent.Id,
                        Name = this.AgentName.Trim(),
                        Description = this.AgentDescription.Trim(),
                        InstructionsPath = this.SelectedAgent.InstructionsPath,
                        PromptyPath = this.SelectedAgent.PromptyPath
                    };

                    this.StatusMessage = "Updating existing agent...";
                }

                AgentDefinition savedAgent = await this._agentService.SaveAgentAsync(agentToSave);

                if (this.SelectedAgent == null)
                {
                    // Add new agent to collection
                    this.Agents.Add(savedAgent);
                    this.SelectedAgent = savedAgent;
                }
                else
                {
                    // Update existing agent in collection
                    int index = this.Agents.IndexOf(this.SelectedAgent);
                    if (index >= 0)
                    {
                        this.Agents[index] = savedAgent;
                        this.SelectedAgent = savedAgent;
                    }
                }

                this.ClearForm();
                this.IsEditing = false;
                this.StatusMessage = "Agent saved successfully";
                AgentChanged?.Invoke();
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error saving agent: {ex.Message}";
            }
            finally
            {
                this.IsLoading = false;
            }
        }

        /// <summary>
        /// Cancels the current edit operation
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            this.ClearForm();
            this.IsEditing = false;
            this.StatusMessage = "Edit cancelled";
            this.HasError = false;
        }

        /// <summary>
        /// Selects an agent for editing
        /// </summary>
        [RelayCommand]
        private void SelectAgent(AgentDefinition agent)
        {
            if (agent != null)
            {
                this.SelectedAgent = agent;
                this.StatusMessage = $"Selected agent: {agent.Name}";
                this.HasError = false;
            }
        }

        /// <summary>
        /// Clears the form fields
        /// </summary>
        private void ClearForm()
        {
            this.AgentName = string.Empty;
            this.AgentDescription = string.Empty;
        }

        /// <summary>
        /// Determines if the save command can be executed
        /// </summary>
        public bool CanSaveAgent => !string.IsNullOrWhiteSpace(this.AgentName) &&
                                   !string.IsNullOrWhiteSpace(this.AgentDescription) &&
                                   !this.IsLoading;

        /// <summary>
        /// Determines if the delete command can be executed
        /// </summary>
        public bool CanDeleteAgent => this.SelectedAgent != null && !this.IsLoading;

        /// <summary>
        /// Determines if the edit command can be executed
        /// </summary>
        public bool CanEditAgent => this.SelectedAgent != null && !this.IsLoading && !this.IsEditing;
    }
}