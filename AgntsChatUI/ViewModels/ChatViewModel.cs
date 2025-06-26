namespace AgntsChatUI.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using AgntsChatUI.AI;
    using AgntsChatUI.Models;

    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    using Microsoft.SemanticKernel.ChatCompletion;

    public partial class ChatViewModel : ViewModelBase
    {
        private readonly DocumentManagementViewModel _documentManagementViewModel;
        private readonly ChatHistory _chatHistory;
        private readonly ChatAgentFactory _agentFactory;

        [ObservableProperty]
        private string messageText = string.Empty;

        [ObservableProperty]
        private bool isTyping;

        [ObservableProperty]
        private bool isLoadingAgents;

        [ObservableProperty]
        private ObservableCollection<AgentDefinition> availableAgents = [];

        [ObservableProperty]
        private AgentDefinition? selectedAgent;

        [ObservableProperty]
        private ChatAgent? currentChatAgent;

        public ObservableCollection<MessageResult> Messages { get; } = [];

        public event Action? ScrollToBottomRequested;

        public ChatViewModel() : this(null)
        {
        }

        public ChatViewModel(DocumentManagementViewModel? documentManagementViewModel)
        {
            this._documentManagementViewModel = documentManagementViewModel ?? new DocumentManagementViewModel();
            this._chatHistory = new ChatHistory();
            this._agentFactory = new ChatAgentFactory();

            this.InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await this.LoadAgentsAsync();
        }

        private async Task LoadAgentsAsync()
        {
            this.IsLoadingAgents = true;

            try
            {
                IEnumerable<ChatAgent> agents = ChatAgentFactory.LoadAgentsFromConfig();
                List<AgentDefinition> agentDefinitions = new List<AgentDefinition>();

                if (File.Exists("agents.config.json"))
                {
                    string config = await File.ReadAllTextAsync("agents.config.json");
                    AgentDefinition[]? definitions = System.Text.Json.JsonSerializer.Deserialize<AgentDefinition[]>(config);
                    if (definitions != null)
                    {
                        agentDefinitions.AddRange(definitions);
                    }
                }

                this.AvailableAgents = new ObservableCollection<AgentDefinition>(agentDefinitions);

                if (this.AvailableAgents.Any())
                {
                    this.SelectedAgent = this.AvailableAgents.First();
                    await this.UpdateCurrentAgentAsync();
                }
            }
            catch (Exception ex)
            {
                AgentDefinition errorAgent = new AgentDefinition
                {
                    Name = "Error Loading Agents",
                    Description = ex.Message
                };
                this.AvailableAgents = new ObservableCollection<AgentDefinition> { errorAgent };
            }
            finally
            {
                this.IsLoadingAgents = false;
            }
        }

        partial void OnSelectedAgentChanged(AgentDefinition? value)
        {
            if (value != null)
            {
                _ = UpdateCurrentAgentAsync();
            }
        }

        private async Task UpdateCurrentAgentAsync()
        {
            if (this.SelectedAgent == null)
            {
                return;
            }

            try
            {
                this.CurrentChatAgent = ChatAgentFactory.CreateAgent(
                    this.SelectedAgent.Name,
                    this.SelectedAgent.Description,
                    this.SelectedAgent.InstructionsPath,
                    this.SelectedAgent.PromptyPath
                );

                if (this.Messages.Any())
                {
                    MessageResult switchMessage = new MessageResult(
                        "🤖",
                        $"Switched to {this.SelectedAgent.Name}. How can I help you?",
                        DateTime.Now,
                        false
                    );
                    this.Messages.Add(switchMessage);
                    ScrollToBottomRequested?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MessageResult errorMessage = new MessageResult(
                    "❌",
                    $"Failed to load agent '{this.SelectedAgent.Name}': {ex.Message}",
                    DateTime.Now,
                    false
                );
                this.Messages.Add(errorMessage);
                ScrollToBottomRequested?.Invoke();
            }
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(this.MessageText) || this.CurrentChatAgent == null)
            {
                return;
            }

            string originalMessage = this.MessageText;
            string messageWithContext = await this.BuildMessageWithDocumentContext(originalMessage);

            MessageResult userMessage = new MessageResult(
                "ME",
                originalMessage,
                DateTime.Now,
                true
            );

            this.Messages.Add(userMessage);
            this._chatHistory.AddUserMessage(messageWithContext);
            this.MessageText = string.Empty;

            ScrollToBottomRequested?.Invoke();

            this.IsTyping = true;
            ScrollToBottomRequested?.Invoke();

            try
            {
                MessageResult botMessage = new MessageResult(
                    "🤖",
                    "",
                    DateTime.Now,
                    false
                );
                this.Messages.Add(botMessage);

                StringBuilder responseBuilder = new StringBuilder();

                await foreach (string chunk in this.CurrentChatAgent.InvokeStreamingAsyncInvokeAsync(messageWithContext, this._chatHistory))
                {
                    responseBuilder.Append(chunk);
                    botMessage.Content = responseBuilder.ToString();

                    if (responseBuilder.Length % 50 == 0)
                    {
                        ScrollToBottomRequested?.Invoke();
                    }
                }

                this._chatHistory.AddAssistantMessage(botMessage.Content);
            }
            catch (Exception ex)
            {
                MessageResult errorMessage = new MessageResult(
                    "❌",
                    $"Error: {ex.Message}",
                    DateTime.Now,
                    false
                );
                this.Messages.Add(errorMessage);
            }
            finally
            {
                this.IsTyping = false;
                ScrollToBottomRequested?.Invoke();
            }
        }

        private async Task<string> BuildMessageWithDocumentContext(string userMessage)
        {
            List<ContextDocument> includedDocuments = this._documentManagementViewModel.Documents
                .Where(doc => doc.IsIncludedInChat)
                .ToList();

            if (!includedDocuments.Any())
            {
                return userMessage;
            }

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(userMessage);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("--- DOCUMENT CONTEXT ---");

            foreach (ContextDocument? document in includedDocuments)
            {
                messageBuilder.AppendLine($"--- {document.Title} ---");

                try
                {
                    string documentText = await this.ReadDocumentText(document.FilePath);
                    messageBuilder.AppendLine(documentText);
                }
                catch (Exception ex)
                {
                    messageBuilder.AppendLine($"[Error reading document: {ex.Message}]");
                }

                messageBuilder.AppendLine();
            }

            return messageBuilder.ToString();
        }

        private async Task<string> ReadDocumentText(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "[Document file not found]";
            }

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".txt" => await File.ReadAllTextAsync(filePath),
                ".pdf" => "[PDF content extraction not implemented - placeholder text]",
                ".doc" or ".docx" => "[Word document content extraction not implemented - placeholder text]",
                ".xls" or ".xlsx" => "[Excel document content extraction not implemented - placeholder text]",
                _ => $"[Content extraction not supported for {extension} files]"
            };
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
    }
}