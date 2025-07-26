## Current State Analysis

- Agents are currently loaded from `agents.config.json` file in `ChatViewModel.LoadAgentsAsync()`
- `AgentDefinition` objects contain: Name, Description, InstructionsPath, PromptyPath, IsSelected
- `ChatAgentFactory.LoadAgentsFromConfig()` handles the JSON deserialization
- The system populates `AvailableAgents` collection for UI binding

## Implementation Plan

### 1. Database Infrastructure

- Add SQLite NuGet package (`Microsoft.Data.Sqlite`) to the project
- Create database schema with single `Agents` table containing:
  - Id (Primary Key, Integer, Auto-increment)
  - Name (Text, Not Null)
  - Description (Text)
  - InstructionsPath (Text)
  - PromptyPath (Text)

### 2. Data Access Layer

- Create `IAgentRepository` interface defining CRUD operations
- Implement `SqliteAgentRepository` class with methods:
  - `GetAllAgentsAsync()` - retrieve all agents
  - `SaveAgentAsync(AgentDefinition)` - insert/update agent
  - `DeleteAgentAsync(int id)` - remove agent
- Add database connection management and initialization logic

### 3. Service Layer Updates

- Create `IAgentService` interface as abstraction layer
- Implement `AgentService` class that uses the repository
- Handle database file creation and initial setup

### 4. Model Updates

- Add `Id` property to `AgentDefinition` class (nullable int for new agents)
- Ensure model is compatible with SQLite data types

### 5. ViewModel Modifications

- Update `ChatViewModel.LoadAgentsAsync()` to use `IAgentService` instead of JSON file reading
- Remove JSON file loading logic
- Inject `IAgentService` via constructor dependency

### 6. Dependency Injection Setup

- Register `IAgentRepository`, `IAgentService` implementations in DI container
- Update `ChatViewModel` constructor to accept `IAgentService`
- Modify `MainWindowViewModel` to pass service to `ChatViewModel`

### 7. Database Initialization

- Create database file in application startup if it doesn't exist
- Add database initialization logic in `App.axaml.cs` or service constructor
- Handle database file location (app directory or user data folder)

### 8. Migration Strategy

- On first run, if the database does not exist, read agent definitions from `agents.config.json`
- Use the JSON file to initialize the agent database with existing agent data
- Retain `agents.config.json` for future reference; do not delete after migration

## Required File Changes

- **AgntsChatUI.csproj**: Add SQLite NuGet package
- **Models/AgentDefinition.cs**: Add Id property  
- **Services/IAgentRepository.cs**: New interface
- **Services/SqliteAgentRepository.cs**: New implementation
- **Services/IAgentService.cs**: New interface
- **Services/AgentService.cs**: New implementation
- **ViewModels/ChatViewModel.cs**: Update LoadAgentsAsync method and constructor
- **ViewModels/MainWindowViewModel.cs**: Update to pass agent service
- **App.axaml.cs**: Add DI registration and database initialization

## Key Considerations

- Database file location and permissions
- Error handling for database operations
- Async/await patterns for database calls
- Maintaining existing UI functionality and data binding

This plan maintains the current functionality while replacing the storage mechanism with SQLite, following SOLID principles and keeping the implementation focused and minimal.
