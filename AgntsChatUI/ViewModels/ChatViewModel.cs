namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;
    using AgntsChatUI.Models;
    using AgntsChatUI.Services;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    using Microsoft.Extensions.Logging;
    using Microsoft.SemanticKernel.ChatCompletion;
    using Avalonia.Threading;

    public partial class ChatViewModel : ViewModelBase
    {
        private readonly ChatHistory _chatHistory;
        private readonly ChatAgentFactory _chatAgentFactory;
        private readonly IAgentService _agentService;
        private readonly IDataSourceManager _dataSourceManager;
        private readonly ILogger<ChatViewModel> _logger;

        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private bool isTyping;

        [ObservableProperty]
        private bool isLoadingAgents;

        [ObservableProperty]
        private bool isKernelConfigurationOpen;

        [ObservableProperty]
        private bool isAgentPanelOpen = false;

        [ObservableProperty]
        private ObservableCollection<AgentDefinition> availableAgents = [];

        [ObservableProperty]
        private ObservableCollection<AgentDefinition> selectedAgents = [];

        [ObservableProperty]
        private bool isOrchestrationRunning = false;

        // Computed property for backward compatibility - returns first selected agent
        public AgentDefinition? SelectedAgent => this.SelectedAgents.FirstOrDefault();

        // Observable property to enable/disable send functionality
        [ObservableProperty]
        private bool canSendMessage = false;

        // Observable properties for status text and color
        [ObservableProperty]
        private string statusText = "● No agents selected - please select at least one agent";

        [ObservableProperty]
        private string statusColor = "#ea4335";

        [ObservableProperty]
        private ChatAgent? currentChatAgent;

        public ObservableCollection<MessageResult> Messages { get; } = [];
        public ObservableCollection<KernelArgument> KernelArguments { get; } = [];

        public event Action? ScrollToBottomRequested;

        public ChatViewModel(IAgentService agentService, IDataSourceManager dataSourceManager, ILogger<ChatViewModel>? logger = null)
        {
            this._chatHistory = new ChatHistory();
            this._chatAgentFactory = new ChatAgentFactory();
            this._agentService = agentService ?? throw new ArgumentNullException(nameof(agentService), "IAgentService is required for ChatViewModel");
            this._dataSourceManager = dataSourceManager ?? throw new ArgumentNullException(nameof(dataSourceManager), "IDataSourceManager is required for ChatViewModel");
            this._logger = logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<ChatViewModel>();
            this.InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await this.LoadAgentsAsync();
        }

        public async Task LoadAgentsAsync()
        {
            this.IsLoadingAgents = true;

            try
            {
                // Remember currently selected agent IDs/names to preserve selection
                HashSet<string> previouslySelectedAgentNames = this.SelectedAgents
                    .Where(a => !string.IsNullOrEmpty(a.Name))
                    .Select(a => a.Name)
                    .ToHashSet();

                // Load agents from the database
                IEnumerable<AgentDefinition> agentDefinitions = await this._agentService.GetAllAgentsAsync();
                List<AgentDefinition> agentList = agentDefinitions.ToList();

                // Clear current collections
                this.AvailableAgents.Clear();
                this.SelectedAgents.Clear();

                if (agentList.Any())
                {
                    // Add all agents to available agents
                    foreach (AgentDefinition agent in agentList)
                    {
                        // Restore selection state if this agent was previously selected
                        if (previouslySelectedAgentNames.Contains(agent.Name))
                        {
                            agent.IsSelected = true;
                            this.SelectedAgents.Add(agent);
                        }
                        else
                        {
                            agent.IsSelected = false;
                        }

                        this.AvailableAgents.Add(agent);
                    }

                    // If no agents were previously selected and we have agents available, select the first one
                    if (!this.SelectedAgents.Any())
                    {
                        AgentDefinition firstAgent = this.AvailableAgents.First();
                        firstAgent.IsSelected = true;
                        this.SelectedAgents.Add(firstAgent);
                        System.Diagnostics.Debug.WriteLine($"LoadAgentsAsync: Auto-selected first agent '{firstAgent.Name}' since no previous selection");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"LoadAgentsAsync: Restored {this.SelectedAgents.Count} previously selected agents");
                    }

                    this.UpdateCurrentAgents();
                }
                else
                {
                    // No agents found in database, show a helpful message
                    AgentDefinition noAgentsAgent = new AgentDefinition
                    {
                        Name = "No Agents Available",
                        Description = "No agents have been configured yet. Please add agents to get started.",
                        InstructionsPath = string.Empty,
                        PromptyPath = string.Empty,
                        IsSelected = false
                    };
                    this.AvailableAgents.Add(noAgentsAgent);
                }

                this.UpdateCanSendMessage();
                this.UpdateStatus();

                System.Diagnostics.Debug.WriteLine($"LoadAgentsAsync completed: {this.AvailableAgents.Count} available, {this.SelectedAgents.Count} selected");
            }
            catch (Exception ex)
            {
                // Clear everything on error
                this.AvailableAgents.Clear();
                this.SelectedAgents.Clear();

                AgentDefinition errorAgent = new AgentDefinition
                {
                    Name = "Error Loading Agents",
                    Description = ex.Message,
                    InstructionsPath = string.Empty,
                    PromptyPath = string.Empty,
                    IsSelected = false
                };
                this.AvailableAgents.Add(errorAgent);

                this.UpdateCanSendMessage();
                this.UpdateStatus();

                System.Diagnostics.Debug.WriteLine($"LoadAgentsAsync error: {ex.Message}");
            }
            finally
            {
                this.IsLoadingAgents = false;
            }
        }

        partial void OnSelectedAgentsChanged(ObservableCollection<AgentDefinition> value)
        {
            if (value.Any())
            {
                UpdateCurrentAgents();
            }

            UpdateCanSendMessage();
            UpdateStatus();
        }

        private void UpdateCurrentAgents()
        {
            if (!this.SelectedAgents.Any())
            {
                return;
            }

            try
            {
                // Update orchestration state and prepare for multi-agent processing
                string agentNames = string.Join(", ", this.SelectedAgents.Select(a => a.Name));

                if (this.Messages.Any())
                {
                    MessageResult switchMessage = new MessageResult(
                        "🤖",
                        $"Orchestration ready with agents: {agentNames}. How can I help you?",
                        DateTime.Now,
                        false
                    );
                    this.Messages.Add(switchMessage);
                    ScrollToBottomRequested?.Invoke();
                }
            }
            catch (Exception ex)
            {
                string agentNames = string.Join(", ", this.SelectedAgents.Select(a => a.Name));
                MessageResult errorMessage = new MessageResult(
                    "❌",
                    $"Failed to setup orchestration with agents '{agentNames}': {ex.Message}",
                    DateTime.Now,
                    false
                );
                this.Messages.Add(errorMessage);
                ScrollToBottomRequested?.Invoke();
            }
        }

        private void UpdateCanSendMessage()
        {
            bool newValue = !string.IsNullOrWhiteSpace(this.MessageText) && this.SelectedAgents.Any() && !this.IsOrchestrationRunning;
            if (this.CanSendMessage != newValue)
            {
                this.CanSendMessage = newValue;
                System.Diagnostics.Debug.WriteLine($"CanSendMessage updated to: {newValue} (MessageText: '{this.MessageText}', SelectedAgents: {this.SelectedAgents.Count}, IsOrchestrationRunning: {this.IsOrchestrationRunning})");
            }
        }

        private void UpdateStatus()
        {
            if (this.SelectedAgents.Count == 0)
            {
                this.StatusText = "● No agents selected - please select at least one agent";
                this.StatusColor = "#ea4335";
            }
            else if (string.IsNullOrWhiteSpace(this.MessageText))
            {
                this.StatusText = $"● {this.SelectedAgents.Count} agent(s) selected - type a message to send";
                this.StatusColor = "#f4b400"; // Yellow for waiting for input
            }
            else if (this.IsOrchestrationRunning)
            {
                this.StatusText = $"● {this.SelectedAgents.Count} agent(s) selected - processing...";
                this.StatusColor = "#4285f4"; // Blue for processing
            }
            else
            {
                this.StatusText = $"● {this.SelectedAgents.Count} agent(s) selected - ready to send";
                this.StatusColor = "#34a853"; // Green for ready
            }

            System.Diagnostics.Debug.WriteLine($"Status updated: {this.StatusText}, Color: {this.StatusColor}, CanSendMessage: {this.CanSendMessage}");
        }

        partial void OnMessageTextChanged(string value)
        {
            this.UpdateCanSendMessage();
            this.UpdateStatus();
        }

        partial void OnIsOrchestrationRunningChanged(bool value)
        {
            this.UpdateCanSendMessage();
            this.UpdateStatus();
        }

        [RelayCommand]
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(this.MessageText) || !this.SelectedAgents.Any())
            {
                return;
            }

            // Capture UI state on the main thread
            string originalMessage = this.MessageText;
            var selectedAgents = this.SelectedAgents.ToList();

            // Clear message text immediately on UI thread
            this.MessageText = string.Empty;

            // Create and add user message on UI thread
            MessageResult userMessage = new MessageResult(
                "ME",
                originalMessage,
                DateTime.Now,
                true
            );

            this.Messages.Add(userMessage);
            ScrollToBottomRequested?.Invoke();

            // Set loading states on UI thread
            this.IsTyping = true;
            this.IsOrchestrationRunning = true;
            ScrollToBottomRequested?.Invoke();

            // Run the heavy work on background thread
            _ = Task.Run(async () =>
            {
                try
                {
                    // Structured JSON log for chat request to Aspire
                    ChatRequestLog chatRequest = new ChatRequestLog(
                        originalMessage,
                        [.. selectedAgents.Select(a => a.Name)],
                        DateTime.UtcNow
                    );

                    this._logger.LogInformation("ChatRequest {@ChatRequest}", JsonSerializer.Serialize(chatRequest, new JsonSerializerOptions { WriteIndented = true }));

                    string messageWithContext = await this.BuildMessageWithDataSourceContext(originalMessage);

                    // Add to chat history
                    this._chatHistory.AddUserMessage(messageWithContext);

                    // Create bot message on UI thread
                    MessageResult botMessage = new MessageResult(
                        "🤖",
                        "",
                        DateTime.Now,
                        false
                    );

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.Messages.Add(botMessage);
                        ScrollToBottomRequested?.Invoke();
                    });

                    StringBuilder responseBuilder = new StringBuilder();
                    Dictionary<string, string> kernelArgs = this.GetKernelArgumentsDictionary();

                    // Create orchestration with selected agents
                    Microsoft.SemanticKernel.Agents.Orchestration.Sequential.SequentialOrchestration orchestration = await this._chatAgentFactory.CreateOrchestration(selectedAgents);

                    // Start runtime and execute orchestration
                    await this._chatAgentFactory.StartRuntimeAsync();

                    try
                    {
                        await foreach (string chunk in this._chatAgentFactory.ExecuteOrchestrationStreamingAsync(
                            orchestration,
                            messageWithContext,
                            this._chatHistory,
                            kernelArgs))
                        {
                            responseBuilder.Append(chunk);
                            
                            // Update UI on main thread
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                botMessage.Content = responseBuilder.ToString();
                                
                                if (responseBuilder.Length % 50 == 0)
                                {
                                    ScrollToBottomRequested?.Invoke();
                                }
                            });
                        }

                        ChatResponseLog chatResponse = new ChatResponseLog(
                            botMessage.Content,
                            selectedAgents.First().Name,
                            DateTime.UtcNow
                        );
                        this._logger.LogInformation("ChatResponse {@ChatResponse}", JsonSerializer.Serialize(chatResponse, new JsonSerializerOptions { WriteIndented = true }));
                    }
                    catch (Exception)
                    {
                        // Fallback to single agent if orchestration fails
                        if (selectedAgents.Count() > 1)
                        {
                            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                botMessage.Content = $"Orchestration failed, falling back to single agent processing...\n\n";
                                ScrollToBottomRequested?.Invoke();
                            });

                            // Use the first selected agent as fallback
                            ChatAgent fallbackAgent = ChatAgentFactory.CreateAgent(
                                selectedAgents.First().Name,
                                selectedAgents.First().Description,
                                selectedAgents.First().InstructionsPath,
                                selectedAgents.First().PromptyPath
                            );

                            await foreach (string chunk in fallbackAgent.InvokeStreamingAsyncInvokeAsync(messageWithContext, this._chatHistory, kernelArgs))
                            {
                                responseBuilder.Append(chunk);
                                
                                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    botMessage.Content = responseBuilder.ToString();

                                    if (responseBuilder.Length % 50 == 0)
                                    {
                                        ScrollToBottomRequested?.Invoke();
                                    }
                                });
                            }
                        }
                        else
                        {
                            // No fallback available, let the outer catch handle it
                            throw; // Re-throw the original exception
                        }
                    }
                    finally
                    {
                        // Ensure runtime is stopped
                        await this._chatAgentFactory.StopRuntimeAsync();
                    }

                    this._chatHistory.AddAssistantMessage(botMessage.Content);
                }
                catch (Exception ex)
                {
                    // Handle errors on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        MessageResult errorMessage = new MessageResult(
                            "❌",
                            $"Orchestration Error: {ex.Message}",
                            DateTime.Now,
                            false
                        );
                        this.Messages.Add(errorMessage);
                    });
                }
                finally
                {
                    // Reset loading states on UI thread
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        this.IsTyping = false;
                        this.IsOrchestrationRunning = false;
                        ScrollToBottomRequested?.Invoke();
                    });
                }
            });
        }

        private Dictionary<string, string> GetKernelArgumentsDictionary()
        {
            return this.KernelArguments
                .Where(arg => !string.IsNullOrWhiteSpace(arg.Key))
                .ToDictionary(arg => arg.Key, arg => arg.Value ?? string.Empty);
        }

        #region Kernel Configuration Commands

        [RelayCommand]
        private void OpenKernelConfiguration()
        {
            this.IsKernelConfigurationOpen = true;
        }

        [RelayCommand]
        private void CloseKernelConfiguration()
        {
            this.IsKernelConfigurationOpen = false;
        }

        [RelayCommand]
        private void ApplyKernelConfiguration()
        {
            this.IsKernelConfigurationOpen = false;

            MessageResult configMessage = new MessageResult(
                "🤖",
                $"Kernel configuration applied with {this.KernelArguments.Count} arguments.",
                DateTime.Now,
                false
            );
            this.Messages.Add(configMessage);
            ScrollToBottomRequested?.Invoke();
        }

        [RelayCommand]
        private void AddKernelArgument()
        {
            this.KernelArguments.Add(new KernelArgument());
        }

        [RelayCommand]
        private void RemoveKernelArgument(KernelArgument argument)
        {
            this.KernelArguments.Remove(argument);
        }

        [RelayCommand]
        private void ResetKernelArguments()
        {
            this.KernelArguments.Clear();
        }

        [RelayCommand]
        private void LoadTemplate(string templateType)
        {
            this.KernelArguments.Clear();

            switch (templateType.ToLowerInvariant())
            {
                case "persona":
                    this.KernelArguments.Add(new KernelArgument("firstName", "", "User's first name for personalization"));
                    this.KernelArguments.Add(new KernelArgument("role", "", "User's professional role or title"));
                    this.KernelArguments.Add(new KernelArgument("expertise", "", "User's area of expertise"));
                    break;

                case "context":
                    this.KernelArguments.Add(new KernelArgument("context", "", "Additional context for the conversation"));
                    this.KernelArguments.Add(new KernelArgument("domain", "", "Specific domain or industry context"));
                    this.KernelArguments.Add(new KernelArgument("objective", "", "Primary objective or goal"));
                    break;

                case "analysis":
                    this.KernelArguments.Add(new KernelArgument("analysisType", "", "Type of analysis to perform"));
                    this.KernelArguments.Add(new KernelArgument("focusArea", "", "Specific area to focus analysis on"));
                    this.KernelArguments.Add(new KernelArgument("outputFormat", "", "Desired format for analysis output"));
                    this.KernelArguments.Add(new KernelArgument("depth", "", "Level of analysis depth required"));
                    break;
            }
        }

        #endregion

        private async Task<string> BuildMessageWithDataSourceContext(string userMessage)
        {
            try
            {
                IEnumerable<DataSourceResult> searchResults = await this._dataSourceManager.SearchAllSourcesAsync(userMessage);

                if (!searchResults.Any())
                {
                    return userMessage;
                }

                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.AppendLine(userMessage);
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("--- DATA SOURCE CONTEXT ---");

                foreach (DataSourceResult result in searchResults)
                {
                    messageBuilder.AppendLine($"--- {result.Title} ({result.SourceName}) ---");
                    messageBuilder.AppendLine(result.Content ?? string.Empty);
                    messageBuilder.AppendLine();
                }

                return messageBuilder.ToString();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the entire message sending
                this._logger.LogError(ex, "Error building message with data source context");
                return userMessage; // Return original message without context
            }
        }

        [RelayCommand]
        private void EditMessage(MessageResult message)
        {
            message.IsEditing = true;
        }

        [RelayCommand]
        private void SaveEdit(MessageResult message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void CancelEdit(MessageResult message)
        {
            message.IsEditing = false;
        }

        [RelayCommand]
        private void DeleteMessage(MessageResult message)
        {
            this.Messages.Remove(message);
        }

        [RelayCommand]
        private void ToggleAgentPanel()
        {
            this.IsAgentPanelOpen = !this.IsAgentPanelOpen;
        }

        [RelayCommand]
        private void QuickAgentSwitch(AgentDefinition agent)
        {
            if (agent != null)
            {
                // Clear current selection and select the new agent
                this.SelectedAgents.Clear();
                foreach (var availableAgent in this.AvailableAgents)
                {
                    availableAgent.IsSelected = false;
                }
                
                agent.IsSelected = true;
                this.SelectedAgents.Add(agent);
                this.OnAgentSelectionChanged(agent);
            }
        }

        [RelayCommand]
        private void RemoveAgentFromSelection(AgentDefinition agent)
        {
            this.RemoveAgentSelection(agent);
        }

        [RelayCommand]
        private void ClearAgentSelection()
        {
            this.SelectedAgents.Clear();
            foreach (var agent in this.AvailableAgents)
            {
                agent.IsSelected = false;
            }
            this.UpdateCanSendMessage();
            this.UpdateStatus();
        }

        // Handle agent selection changes through property binding
        public void OnAgentSelectionChanged(AgentDefinition agent)
        {
            System.Diagnostics.Debug.WriteLine($"OnAgentSelectionChanged called for agent: {agent.Name}, IsSelected: {agent.IsSelected}");

            if (agent.IsSelected)
            {
                if (!this.SelectedAgents.Contains(agent))
                {
                    this.SelectedAgents.Add(agent);
                    System.Diagnostics.Debug.WriteLine($"Added agent: {agent.Name}, Total selected: {this.SelectedAgents.Count}");
                }
            }
            else
            {
                this.SelectedAgents.Remove(agent);
                System.Diagnostics.Debug.WriteLine($"Removed agent: {agent.Name}, Total selected: {this.SelectedAgents.Count}");
            }

            this.UpdateCanSendMessage();
            this.UpdateStatus();
        }

        public void RemoveAgentSelection(AgentDefinition? agent)
        {
            if (agent == null)
            {
                return;
            }

            this.SelectedAgents.Remove(agent);
            agent.IsSelected = false;
            this.UpdateCanSendMessage();
            this.UpdateStatus();
        }
    }
}