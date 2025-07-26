# External Data Sources Implementation Plan - Guided Implementation

## Task Overview

Transform the existing document-specific system into a modular, extensible data source architecture. This involves creating a unified search abstraction, replacing document management with generic data source management, and updating the chat system to use search-based context injection. All existing documents will be lost as no migration is provided.

---

## Foundation Phase

### Sub-phase 1: Core Abstractions

**Dependencies:** None

#### New Models/IDataSource.cs

```csharp
namespace AgntsChatUI.Services
{
    // Core interface for all data source implementations
    public interface IDataSource
    {
        string Name { get; }
        string Description { get; }
        bool IsEnabled { get; set; }
        
        // Primary search method - returns full content for documents
        Task<IEnumerable<DataSourceResult>> SearchAsync(string query, Dictionary<string, object> parameters = null);
        
        // Configuration validation
        Task<bool> ValidateConfigurationAsync();
    }
}
```

#### New Models/DataSourceResult.cs

```csharp
namespace AgntsChatUI.Models
{
    // Unified result model for all data source searches
    public class DataSourceResult
    {
        public string Content { get; set; }        // Full content (for documents) or relevant excerpt
        public string Title { get; set; }          // Display title
        public string SourceName { get; set; }     // Name of the data source
        public string SourceType { get; set; }     // Type identifier
        public Dictionary<string, object> Metadata { get; set; } // Additional context
        public DateTime RetrievedAt { get; set; }
    }
}
```

#### New Models/DataSourceDefinition.cs

```csharp
namespace AgntsChatUI.Models
{
    // Database entity for storing data source configurations
    public partial class DataSourceDefinition : ObservableObject
    {
        public int? Id { get; set; }
        
        [ObservableProperty]
        private string name = string.Empty;
        
        [ObservableProperty]
        private string description = string.Empty;
        
        public DataSourceType Type { get; set; }
        public string ConfigurationJson { get; set; } = string.Empty;
        
        [ObservableProperty]
        private bool isEnabled = true;
        
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
```

#### New Models/DataSourceType.cs

```csharp
namespace AgntsChatUI.Models
{
    // Enumeration of supported data source types
    public enum DataSourceType
    {
        LocalFiles,
        WebApi,
        Database,
        SharePoint,
        Custom
    }
}
```

---

### Sub-phase 2: Database Layer

**Dependencies:** Sub-phase 1 (DataSourceDefinition)

#### New Services/IDataSourceRepository.cs

```csharp
namespace AgntsChatUI.Services
{
    // Data access interface for data source persistence
    public interface IDataSourceRepository
    {
        Task InitializeDatabaseAsync();
        Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync();
        Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource);
        Task<bool> DeleteDataSourceAsync(int id);
        Task<DataSourceDefinition?> GetDataSourceByIdAsync(int id);
    }
}
```

#### New Services/SqliteDataSourceRepository.cs

```csharp
namespace AgntsChatUI.Services
{
    // SQLite implementation of data source repository
    public class SqliteDataSourceRepository : IDataSourceRepository
    {
        private readonly string _connectionString;
        
        public SqliteDataSourceRepository()
        {
            // Use same database as agents
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AgntsChatUI");
            Directory.CreateDirectory(appDataPath);
            
            string databasePath = Path.Combine(appDataPath, "agents.db");
            _connectionString = $"Data Source={databasePath}";
        }
        
        public async Task InitializeDatabaseAsync()
        {
            // Create DataSources table with columns:
            // Id, Name, Description, Type, ConfigurationJson, IsEnabled, DateCreated, DateModified
        }
        
        // Implement CRUD operations...
    }
}
```

#### Modified Services/SqliteAgentRepository.cs

**Impact:** Existing database initialization
**Dependencies:** Uses same database file

```csharp
public async Task InitializeDatabaseAsync()
{
    // Existing Agents table creation...
    
    // ADD: DataSources table creation
    string createDataSourcesTableCommand = @"
        CREATE TABLE IF NOT EXISTS DataSources (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Description TEXT,
            Type INTEGER NOT NULL,
            ConfigurationJson TEXT,
            IsEnabled BOOLEAN DEFAULT 1,
            DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP,
            DateModified DATETIME DEFAULT CURRENT_TIMESTAMP
        )";
    // Execute command...
}
```

---

### Sub-phase 3: Service Layer

**Dependencies:** Sub-phase 1 & 2 (Abstractions and Repository)

#### New Services/IDataSourceService.cs

```csharp
namespace AgntsChatUI.Services
{
    // Business logic interface for data source management
    public interface IDataSourceService
    {
        Task InitializeAsync();
        Task<IEnumerable<DataSourceDefinition>> GetAllDataSourcesAsync();
        Task<DataSourceDefinition> SaveDataSourceAsync(DataSourceDefinition dataSource);
        Task<bool> DeleteDataSourceAsync(int id);
        Task<bool> ValidateDataSourceAsync(DataSourceDefinition dataSource);
    }
}
```

#### New Services/DataSourceService.cs

```csharp
namespace AgntsChatUI.Services
{
    // Business logic implementation with validation and configuration handling
    public class DataSourceService : IDataSourceService
    {
        private readonly IDataSourceRepository _repository;
        
        public DataSourceService(IDataSourceRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        
        // Implement business logic, validation, JSON configuration handling...
    }
}
```

#### New Services/IDataSourceManager.cs

```csharp
namespace AgntsChatUI.Services
{
    // Runtime management of active data sources and search coordination
    public interface IDataSourceManager
    {
        Task InitializeAsync();
        Task<IEnumerable<DataSourceResult>> SearchAllSourcesAsync(string query, Dictionary<string, object> parameters = null);
        Task<IEnumerable<IDataSource>> GetActiveDataSourcesAsync();
        Task RefreshDataSourcesAsync();
    }
}
```

#### New Services/DataSourceManager.cs

```csharp
namespace AgntsChatUI.Services
{
    // Factory-based instantiation and search coordination across all sources
    public class DataSourceManager : IDataSourceManager
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<int, IDataSource> _activeDataSources = new();
        
        public DataSourceManager(IDataSourceService dataSourceService, IServiceProvider serviceProvider)
        {
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
        
        public async Task<IEnumerable<DataSourceResult>> SearchAllSourcesAsync(string query, Dictionary<string, object> parameters = null)
        {
            // Coordinate search across all enabled data sources
            // Aggregate results with source attribution
        }
        
        // Implement factory creation, lifecycle management...
    }
}
```

---

### Sub-phase 4: Data Source Implementations

**Dependencies:** Sub-phase 1 (IDataSource interface)

#### New Services/DataSourceFactory.cs

```csharp
namespace AgntsChatUI.Services
{
    // Factory for creating data source instances by type
    public static class DataSourceFactory
    {
        public static IDataSource CreateDataSource(DataSourceDefinition definition, IServiceProvider serviceProvider)
        {
            return definition.Type switch
            {
                DataSourceType.LocalFiles => new LocalFilesDataSource(definition),
                DataSourceType.WebApi => new WebApiDataSource(definition),
                // Add more types as implemented...
                _ => throw new NotSupportedException($"Data source type {definition.Type} is not supported")
            };
        }
    }
}
```

#### New Services/LocalFilesDataSource.cs

```csharp
namespace AgntsChatUI.Services
{
    // File system search implementation returning full document content
    public class LocalFilesDataSource : IDataSource
    {
        private readonly LocalFilesConfiguration _configuration;
        
        public string Name { get; }
        public string Description { get; }
        public bool IsEnabled { get; set; }
        
        public LocalFilesDataSource(DataSourceDefinition definition)
        {
            Name = definition.Name;
            Description = definition.Description;
            IsEnabled = definition.IsEnabled;
            
            // Deserialize configuration from JSON
            _configuration = JsonSerializer.Deserialize<LocalFilesConfiguration>(definition.ConfigurationJson) 
                ?? new LocalFilesConfiguration();
        }
        
        public async Task<IEnumerable<DataSourceResult>> SearchAsync(string query, Dictionary<string, object> parameters = null)
        {
            // Search files in configured directory
            // Return full file content as single result per file
            // Include file metadata (size, date, type)
        }
    }
}
```

#### New Models/LocalFilesConfiguration.cs

```csharp
namespace AgntsChatUI.Models
{
    // Configuration model for local file data source
    public class LocalFilesConfiguration
    {
        public string DirectoryPath { get; set; } = "";
        public string[] SupportedExtensions { get; set; } = [".txt", ".pdf", ".doc", ".docx"];
        public bool IncludeSubdirectories { get; set; } = true;
        public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    }
}
```

---

## Integration Phase

### Sub-phase 1: Service Registration

**Dependencies:** All Foundation phases

#### Modified App.axaml.cs

**Impact:** Dependency injection container setup
**Dependencies:** All new services must be registered

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Existing agent services...
    
    // ADD: Data source services
    services.AddSingleton<IDataSourceRepository, SqliteDataSourceRepository>();
    services.AddSingleton<IDataSourceService, DataSourceService>();
    services.AddSingleton<IDataSourceManager, DataSourceManager>();
    
    // REMOVE: Document services
    // services.AddSingleton<IDocumentService, DocumentService>(); // DELETE THIS LINE
    
    // Modified ViewModels
    services.AddTransient<MainWindowViewModel>();
    services.AddTransient<ChatViewModel>();
    services.AddTransient<DataSourceManagementViewModel>(); // NEW
    // services.AddTransient<DocumentManagementViewModel>(); // DELETE THIS LINE
    services.AddTransient<AgentManagementViewModel>();
}
```

---

### Sub-phase 2: ViewModel Replacement

**Dependencies:** Sub-phase 1 and all Foundation phases

#### Removed ViewModels/DocumentManagementViewModel.cs

**Impact:** Complete removal - all document management functionality replaced
**Dependencies:**

- `MainWindowViewModel` - remove dependency and references
- `ChatViewModel` - remove document context building

#### New ViewModels/DataSourceManagementViewModel.cs

```csharp
namespace AgntsChatUI.ViewModels
{
    // CRUD operations for data sources, enable/disable toggles
    public partial class DataSourceManagementViewModel : ViewModelBase
    {
        private readonly IDataSourceService _dataSourceService;
        private readonly IDataSourceManager _dataSourceManager;
        
        [ObservableProperty]
        private ObservableCollection<DataSourceDefinition> dataSources = [];
        
        [ObservableProperty]
        private DataSourceDefinition? selectedDataSource;
        
        [ObservableProperty]
        private bool isEditing;
        
        public DataSourceManagementViewModel(IDataSourceService dataSourceService, IDataSourceManager dataSourceManager)
        {
            _dataSourceService = dataSourceService ?? throw new ArgumentNullException(nameof(dataSourceService));
            _dataSourceManager = dataSourceManager ?? throw new ArgumentNullException(nameof(dataSourceManager));
            
            InitializeAsync();
        }
        
        // Implement CRUD commands, configuration dialogs, enable/disable toggles...
        [RelayCommand]
        private async Task AddDataSource() { /* Implementation */ }
        
        [RelayCommand]
        private async Task EditDataSource() { /* Implementation */ }
        
        [RelayCommand]
        private async Task DeleteDataSource() { /* Implementation */ }
        
        [RelayCommand]
        private async Task ToggleDataSource(DataSourceDefinition dataSource) { /* Implementation */ }
    }
}
```

#### Modified ViewModels/ChatViewModel.cs

**Impact:** Major refactoring of context building and dependencies
**Dependencies:** Remove `DocumentManagementViewModel`, add `IDataSourceManager`

```csharp
public partial class ChatViewModel : ViewModelBase
{
    // REMOVE: private readonly DocumentManagementViewModel _documentManagementViewModel;
    // ADD: private readonly IDataSourceManager _dataSourceManager;
    
    private readonly ChatHistory _chatHistory;
    private readonly ChatAgentFactory _chatAgentFactory;
    private readonly IAgentService _agentService;
    
    // REMOVE: Constructor taking DocumentManagementViewModel
    // MODIFY: Constructor to take IDataSourceManager
    public ChatViewModel(IAgentService agentService, IDataSourceManager dataSourceManager)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));
        _dataSourceManager = dataSourceManager ?? throw new ArgumentNullException(nameof(dataSourceManager));
        // Initialize other components...
    }
    
    // REPLACE: BuildMessageWithDocumentContext method
    private async Task<string> BuildMessageWithDataSourceContext(string userMessage)
    {
        // Use _dataSourceManager.SearchAllSourcesAsync(userMessage) instead of document inclusion
        // Build context from search results across all enabled data sources
        
        IEnumerable<DataSourceResult> searchResults = await _dataSourceManager.SearchAllSourcesAsync(userMessage);
        
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
            messageBuilder.AppendLine(result.Content);
            messageBuilder.AppendLine();
        }
        
        return messageBuilder.ToString();
    }
    
    // MODIFY: SendMessage method to use new context building
    [RelayCommand]
    private async Task SendMessage()
    {
        // Replace BuildMessageWithDocumentContext call with BuildMessageWithDataSourceContext
        string messageWithContext = await BuildMessageWithDataSourceContext(originalMessage);
        // Rest of the method remains the same...
    }
}
```

#### Modified ViewModels/MainWindowViewModel.cs

**Impact:** Constructor and property changes
**Dependencies:** Remove `DocumentManagementViewModel`, add `DataSourceManagementViewModel`

```csharp
public partial class MainWindowViewModel : ViewModelBase
{
    // REPLACE: public DocumentManagementViewModel DocumentManagementViewModel { get; }
    public DataSourceManagementViewModel DataSourceManagementViewModel { get; }
    
    public ChatViewModel ChatViewModel { get; }
    public AgentManagementViewModel AgentManagementViewModel { get; }
    
    // MODIFY: Constructor parameters and initialization
    public MainWindowViewModel(IAgentService agentService, IFileTemplateService fileTemplateService, 
        AgentManagementViewModel agentManagementViewModel, DataSourceManagementViewModel dataSourceManagementViewModel,
        IDataSourceManager dataSourceManager)
    {
        // REMOVE: DocumentManagementViewModel = new DocumentManagementViewModel();
        DataSourceManagementViewModel = dataSourceManagementViewModel;
        
        // MODIFY: ChatViewModel constructor call
        ChatViewModel = new ChatViewModel(agentService, dataSourceManager);
        
        AgentManagementViewModel = agentManagementViewModel;
        
        // REMOVE: Document selection event subscription
        // DocumentManagementViewModel.DocumentSelected += OnDocumentSelected;
        
        // Existing agent change subscription remains
        AgentManagementViewModel.AgentChanged += OnAgentChanged;
    }
    
    // REMOVE: OnDocumentSelected method
    // private void OnDocumentSelected(ContextDocument document) { }
}
```

---

### Sub-phase 3: View Updates

**Dependencies:** Sub-phase 2 (ViewModel changes)

#### Removed Files

**Complete removal with no replacement:**

- `Views/DocumentManagementView.axaml`
- `Views/DocumentManagementView.axaml.cs`
- `Models/ContextDocument.cs`
- `Models/DocumentTypeHelper.cs`
- `Models/UploadedDocumentType.cs`
- `Services/DocumentService.cs`
- `Services/IDocumentService.cs`

#### New Views/DataSourceManagementView.axaml

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:AgntsChatUI.ViewModels"
             x:Class="AgntsChatUI.Views.DataSourceManagementView"
             x:DataType="vm:DataSourceManagementViewModel">

    <Border Background="#f8f9fa" BorderBrush="#e8eaed" BorderThickness="0,0,1,0">
        <Grid>
            <!-- Header for Data Source Management -->
            <!-- List of configured data sources with enable/disable toggles -->
            <!-- Add/Edit/Delete buttons for data source configuration -->
            <!-- Configuration dialogs for different data source types -->
        </Grid>
    </Border>
</UserControl>
```

#### New Views/DataSourceManagementView.axaml.cs

```csharp
namespace AgntsChatUI.Views
{
    public partial class DataSourceManagementView : UserControl
    {
        public DataSourceManagementView()
        {
            InitializeComponent();
        }
        
        public DataSourceManagementView(DataSourceManagementViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
```

#### Modified Views/MainWindow.axaml

**Impact:** Replace document management panel with data source management
**Dependencies:** New DataSourceManagementView must exist

```xml
<Grid IsEnabled="True">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="350"/> <!-- Agent Management -->
        <ColumnDefinition Width="380"/> <!-- Data Source Management (was Document Management) -->
        <ColumnDefinition Width="*"/>   <!-- Chat Area -->
    </Grid.ColumnDefinitions>

    <!-- Agent Management Component (unchanged) -->
    <views:AgentManagementView Grid.Column="0" DataContext="{Binding AgentManagementViewModel}"/>

    <!-- REPLACE: Document Management with Data Source Management -->
    <!-- OLD: <views:DocumentManagementView Grid.Column="1" DataContext="{Binding DocumentManagementViewModel}"/> -->
    <views:DataSourceManagementView Grid.Column="1" DataContext="{Binding DataSourceManagementViewModel}"/>

    <!-- Chat Area (unchanged) -->
    <Grid Grid.Column="2" IsEnabled="True">
        <!-- Existing chat components remain the same -->
    </Grid>
</Grid>
```

---

## Summary of Breaking Changes

**Removed Functionality:**

- All existing documents will be lost (no migration)
- Document upload, title editing, and file management UI
- Document-specific models and services

**New Functionality:**

- Generic data source management with SQLite persistence
- Search-based context injection instead of document inclusion
- Modular architecture for adding new data source types
- Local files become one configurable data source among many

**Migration Impact:**

- Users must reconfigure their document sources as "Local Files" data sources
- Existing chat history remains intact
- Agent configurations remain unchanged
