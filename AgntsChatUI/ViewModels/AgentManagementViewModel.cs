namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
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
        private string agentInstructions = string.Empty;

        [ObservableProperty]
        private string agentPrompty = string.Empty;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;

        [ObservableProperty]
        private bool canSaveAgent;

        [ObservableProperty]
        private bool isExpanded = false; // Collapsed by default

        public event Action? AgentChanged;

        public AgentManagementViewModel(IAgentService agentService, IFileTemplateService fileTemplateService)
        {
            this._agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
            this._fileTemplateService = fileTemplateService ?? throw new ArgumentNullException(nameof(fileTemplateService));

            // Initialize CanSaveAgent
            this.UpdateCanSaveAgent();

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
            this.SelectedAgent = null;  // Clear selected agent for new creation
            this.ClearForm();
            this.LoadDefaultTemplates();
            this.IsEditing = true;
            this.StatusMessage = "Adding new agent...";
            this.HasError = false;
            this.UpdateCanSaveAgent();
        }

        /// <summary>
        /// Starts editing the selected agent
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditAgent))]
        private async Task EditAgent(AgentDefinition? agent = null)
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

            // Load existing file content if files exist
            await this.LoadExistingFileContent(agentToEdit);

            this.IsEditing = true;
            this.StatusMessage = $"Editing agent: {agentToEdit.Name}";
            this.HasError = false;
            this.UpdateCanSaveAgent();
        }

        /// <summary>
        /// Deletes the selected agent
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteAgent))]
        private async Task DeleteAgent(AgentDefinition? agent = null)
        {
            AgentDefinition? agentToDelete = agent ?? this.SelectedAgent;
            if (agentToDelete == null || agentToDelete.Id == null)
            {
                this.StatusMessage = "Please select an agent to delete";
                this.HasError = true;
                return;
            }

            try
            {
                this.IsLoading = true;
                this.HasError = false;
                this.StatusMessage = $"Deleting agent: {agentToDelete.Name}...";

                bool success = await this._agentService.DeleteAgentAsync(agentToDelete.Id.Value);

                if (success)
                {
                    this.Agents.Remove(agentToDelete);
                    if (this.SelectedAgent?.Id == agentToDelete.Id)
                    {
                        this.SelectedAgent = null;
                    }

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
                    System.Diagnostics.Debug.WriteLine($"Creating NEW agent: {this.AgentName}");
                    this.StatusMessage = "Creating files and saving new agent...";

                    string instructionPath = await this.CreateInstructionFile(this.AgentName, this.AgentInstructions);
                    string personaPath = await this.CreatePromptyFile(this.AgentName, this.AgentPrompty);

                    agentToSave = new AgentDefinition
                    {
                        Name = this.AgentName.Trim() + (this.AgentName.EndsWith("Agent") ? "" : "Agent"),
                        Description = this.AgentDescription.Trim(),
                        InstructionsPath = instructionPath,
                        PromptyPath = personaPath
                    };
                }
                else
                {
                    // Updating existing agent
                    System.Diagnostics.Debug.WriteLine($"Updating EXISTING agent: {this.SelectedAgent.Name} (ID: {this.SelectedAgent.Id})");
                    this.StatusMessage = "Updating files and saving agent...";

                    string instructionPath = await this.UpdateInstructionFile(this.SelectedAgent, this.AgentInstructions);
                    string personaPath = await this.UpdatePromptyFile(this.SelectedAgent, this.AgentPrompty);

                    agentToSave = new AgentDefinition
                    {
                        Id = this.SelectedAgent.Id,
                        Name = this.AgentName.Trim() + (this.AgentName.EndsWith("Agent") ? "" : "Agent"),
                        Description = this.AgentDescription.Trim(),
                        InstructionsPath = instructionPath,
                        PromptyPath = personaPath
                    };
                }

                AgentDefinition savedAgent = await this._agentService.SaveAgentAsync(agentToSave);

                if (this.SelectedAgent == null)
                {
                    // Add new agent to collection
                    System.Diagnostics.Debug.WriteLine($"Adding new agent to collection: {savedAgent.Name} (ID: {savedAgent.Id})");
                    this.Agents.Add(savedAgent);
                    // Don't set SelectedAgent for new agents to allow immediate creation of another
                }
                else
                {
                    // Update existing agent in collection
                    System.Diagnostics.Debug.WriteLine($"Updating existing agent in collection: {savedAgent.Name} (ID: {savedAgent.Id})");
                    int index = this.Agents.IndexOf(this.SelectedAgent);
                    if (index >= 0)
                    {
                        this.Agents[index] = savedAgent;
                        this.SelectedAgent = savedAgent;  // Keep selection for updated agents
                    }
                }

                this.ClearForm();
                this.SelectedAgent = null;  // Always clear selection after save to allow new agent creation
                this.IsEditing = false;
                this.StatusMessage = "Agent saved successfully";
                AgentChanged?.Invoke();
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error saving agent: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error saving agent: {ex}");
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
            this.SelectedAgent = null;  // Clear selection when canceling
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
        /// Loads instruction template
        /// </summary>
        [RelayCommand]
        private void LoadInstructionTemplate()
        {
            this.AgentInstructions = this.GenerateInstructionTemplate(this.AgentName, this.AgentDescription);
        }

        /// <summary>
        /// Loads prompty template
        /// </summary>
        [RelayCommand]
        private void LoadPromptyTemplate()
        {
            this.AgentPrompty = this.GeneratePromptyTemplate(this.AgentName, this.AgentDescription);
        }

        /// <summary>
        /// Toggles the expanded/collapsed state of the panel
        /// </summary>
        [RelayCommand]
        private void ToggleExpanded()
        {
            this.IsExpanded = !this.IsExpanded;
        }

        /// <summary>
        /// Clears the form fields
        /// </summary>
        private void ClearForm()
        {
            this.AgentName = string.Empty;
            this.AgentDescription = string.Empty;
            this.AgentInstructions = string.Empty;
            this.AgentPrompty = string.Empty;
            this.UpdateCanSaveAgent();
        }

        /// <summary>
        /// Loads default templates for new agents
        /// </summary>
        private void LoadDefaultTemplates()
        {
            this.AgentInstructions = this.GenerateInstructionTemplate("", "");
            this.AgentPrompty = this.GeneratePromptyTemplate("", "");
        }

        /// <summary>
        /// Loads existing file content when editing an agent
        /// </summary>
        private async Task LoadExistingFileContent(AgentDefinition agent)
        {
            try
            {
                // Load instructions file
                this.AgentInstructions = !string.IsNullOrEmpty(agent.InstructionsPath) && File.Exists(agent.InstructionsPath)
                    ? await File.ReadAllTextAsync(agent.InstructionsPath)
                    : this.GenerateInstructionTemplate(agent.Name, agent.Description);

                // Load prompty file
                this.AgentPrompty = !string.IsNullOrEmpty(agent.PromptyPath) && File.Exists(agent.PromptyPath)
                    ? await File.ReadAllTextAsync(agent.PromptyPath)
                    : this.GeneratePromptyTemplate(agent.Name, agent.Description);
            }
            catch (Exception ex)
            {
                this.HasError = true;
                this.StatusMessage = $"Error loading existing files: {ex.Message}";
            }
        }

        /// <summary>
        /// Creates instruction file with custom content
        /// </summary>
        private async Task<string> CreateInstructionFile(string agentName, string content)
        {
            await this._fileTemplateService.EnsureDirectoriesExistAsync();

            string fileName = $"{this.SanitizeFileName(agentName)}.md";
            string baseDir = this._fileTemplateService.GetBaseDirectory();
            string filePath = Path.Combine(baseDir, "Instructions", fileName);

            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Creates prompty file with custom content
        /// </summary>
        private async Task<string> CreatePromptyFile(string agentName, string content)
        {
            await this._fileTemplateService.EnsureDirectoriesExistAsync();

            string fileName = $"{this.SanitizeFileName(agentName)}.prompty";
            string baseDir = this._fileTemplateService.GetBaseDirectory();
            string filePath = Path.Combine(baseDir, "Personas", fileName);

            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Updates instruction file with new content
        /// </summary>
        private async Task<string> UpdateInstructionFile(AgentDefinition agent, string content)
        {
            string filePath = agent.InstructionsPath;

            if (string.IsNullOrEmpty(filePath))
            {
                // Create new file if path doesn't exist
                return await this.CreateInstructionFile(agent.Name, content);
            }

            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Updates prompty file with new content
        /// </summary>
        private async Task<string> UpdatePromptyFile(AgentDefinition agent, string content)
        {
            string filePath = agent.PromptyPath;

            if (string.IsNullOrEmpty(filePath))
            {
                // Create new file if path doesn't exist
                return await this.CreatePromptyFile(agent.Name, content);
            }

            await File.WriteAllTextAsync(filePath, content);
            return filePath;
        }

        /// <summary>
        /// Generates instruction template content
        /// </summary>
        private string GenerateInstructionTemplate(string agentName, string description)
        {
            string name = string.IsNullOrEmpty(agentName) ? "[Agent Name]" : agentName;
            string desc = string.IsNullOrEmpty(description) ? "[Agent Description]" : description;

            return $@"# {name} Instructions

{desc}

## Purpose
This agent is designed to {desc.ToLower()}.

## Guidelines
- Follow the agent's specific purpose and capabilities
- Maintain consistency with the defined persona
- Provide helpful and accurate responses
- Use appropriate language and tone for the context

## Capabilities
- {desc}
- Respond to user queries within the agent's scope
- Maintain context throughout conversations

## Limitations
- Stay within the defined scope and purpose
- Do not perform tasks outside of the agent's capabilities
- Refer users to appropriate resources when needed";
        }

        /// <summary>
        /// Generates prompty template content
        /// </summary>
        private string GeneratePromptyTemplate(string agentName, string description)
        {
            string sanitizedName = string.IsNullOrEmpty(agentName) ? "agent_name" : this.SanitizeFileName(agentName);
            string name = string.IsNullOrEmpty(agentName) ? "[Agent Name]" : agentName;
            string desc = string.IsNullOrEmpty(description) ? "[Agent Description]" : description;

            return $@"---
name: {sanitizedName}
description: {desc}
authors:
  - System Generated
model:
  api: chat
  configuration:
    type: azure_openai
    azure_deployment: ""gpt-4o""
    azure_endpoint: ${{env:AOAI_ENDPOINT}}
    api_key: ${{env:AOAI_API_KEY}}
  parameters:
    max_tokens: 4000
sample: |
  User: Hello, I need help with {desc.ToLower()}.
  Assistant: I'm here to help you with {desc.ToLower()}. What specific assistance do you need?
---

system:
# {name}

{desc}

## Agent Context
### Purpose
This agent is designed to {desc.ToLower()}.

### Capabilities
- {desc}
- Provide helpful and accurate responses
- Maintain context throughout conversations
- Follow established guidelines and best practices

### Guidelines
- Stay focused on the agent's specific purpose
- Use appropriate language and tone
- Provide clear and actionable responses
- Ask clarifying questions when needed

### Limitations
- Stay within the defined scope and purpose
- Do not perform tasks outside of the agent's capabilities
- Refer users to appropriate resources when needed

## Response Format
Provide clear, helpful responses that align with the agent's purpose and capabilities. Use appropriate formatting and structure to make information easy to understand.";
        }

        /// <summary>
        /// Sanitizes filename for file system compatibility
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return "unnamed_agent";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = fileName;

            foreach (char invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            sanitized = sanitized.Replace(' ', '_');
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }

            return sanitized.Trim('_');
        }

        /// <summary>
        /// Updates the CanSaveAgent property based on current form state
        /// </summary>
        private void UpdateCanSaveAgent()
        {
            this.CanSaveAgent = !string.IsNullOrWhiteSpace(this.AgentName) &&
                               !string.IsNullOrWhiteSpace(this.AgentDescription) &&
                               !this.IsLoading;
        }

        /// <summary>
        /// Determines if the delete command can be executed
        /// </summary>
        public bool CanDeleteAgent(AgentDefinition? agent)
        {
            AgentDefinition? targetAgent = agent ?? this.SelectedAgent;
            return targetAgent != null && !this.IsLoading;
        }

        /// <summary>
        /// Determines if the edit command can be executed
        /// </summary>
        public bool CanEditAgent(AgentDefinition? agent)
        {
            AgentDefinition? targetAgent = agent ?? this.SelectedAgent;
            return targetAgent != null && !this.IsLoading && !this.IsEditing;
        }

        // Property change notifications for dependent properties
        partial void OnSelectedAgentChanged(AgentDefinition? value)
        {
            this.EditAgentCommand.NotifyCanExecuteChanged();
            this.DeleteAgentCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsLoadingChanged(bool value)
        {
            this.UpdateCanSaveAgent();
            this.EditAgentCommand.NotifyCanExecuteChanged();
            this.DeleteAgentCommand.NotifyCanExecuteChanged();
        }

        partial void OnIsEditingChanged(bool value)
        {
            this.EditAgentCommand.NotifyCanExecuteChanged();
        }

        partial void OnAgentNameChanged(string value)
        {
            this.UpdateCanSaveAgent();
        }

        partial void OnAgentDescriptionChanged(string value)
        {
            this.UpdateCanSaveAgent();
        }
    }
}