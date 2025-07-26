# Agent Management Implementation Plan

## Task Summary

Create an agent management interface that allows users to create, edit, and delete agents from the database. The system will automatically generate template files in `PromptTemplates\Instructions\` and `PromptTemplates\Personas\` directories when creating new agents, populating the InstructionsPath and PromptyPath properties accordingly. This provides a streamlined workflow for agent configuration without manual file management.

## Phase 1: Core Service Infrastructure

### Step 1.1: Create Template File Service Interface

- **Add**: `AgntsChatUI/Services/IFileTemplateService.cs`
  - Interface with methods: CreateInstructionFile, CreatePersonaFile, EnsureDirectoriesExist

### Step 1.2: Implement Template File Service

- **Add**: `AgntsChatUI/Services/FileTemplateService.cs`
  - Implementation creating files in `PromptTemplates\Instructions\` and `PromptTemplates\Personas\`
  - Methods to generate default content based on agent name/description

### Step 1.3: Register Service in DI Container

- **Modify**: `AgntsChatUI/App.axaml.cs`
  - Add `services.AddSingleton<IFileTemplateService, FileTemplateService>();` in ConfigureServices method

## Phase 2: Agent Management ViewModel

### Step 2.1: Create Agent Management ViewModel

- **Add**: `AgntsChatUI/ViewModels/AgentManagementViewModel.cs`
  - Properties: Agents collection, SelectedAgent, IsEditing, form fields
  - Commands: AddAgent, EditAgent, DeleteAgent, SaveAgent, CancelEdit
  - Dependency on IAgentService and IFileTemplateService

## Phase 3: UI Components

### Step 3.1: Create Agent List Component

- **Add**: `AgntsChatUI/Views/Components/AgentListComponent.axaml`
- **Add**: `AgntsChatUI/Views/Components/AgentListComponent.axaml.cs`
  - Display agents in list with edit/delete buttons
  - Handle selection events

### Step 3.2: Create Agent Form Component

- **Add**: `AgntsChatUI/Views/Components/AgentFormComponent.axaml`
- **Add**: `AgntsChatUI/Views/Components/AgentFormComponent.axaml.cs`
  - Form fields: Name, Description
  - Save/Cancel buttons
  - Validation for required fields

### Step 3.3: Create Main Agent Management View

- **Add**: `AgntsChatUI/Views/AgentManagementView.axaml`
- **Add**: `AgntsChatUI/Views/AgentManagementView.axaml.cs`
  - Layout with AgentListComponent and AgentFormComponent
  - Header with "Add New Agent" button

## Phase 4: Main Window Integration

### Step 4.1: Update Main Window ViewModel

- **Modify**: `AgntsChatUI/ViewModels/MainWindowViewModel.cs`
  - Add `AgentManagementViewModel` property
  - Update constructor to accept and initialize AgentManagementViewModel

### Step 4.2: Update Main Window Layout

- **Modify**: `AgntsChatUI/Views/MainWindow.axaml`
  - Change from 2-column to 3-column layout OR add TabView
  - Add AgentManagementView as new column/tab

### Step 4.3: Update DI Registration

- **Modify**: `AgntsChatUI/App.axaml.cs`
  - Add `services.AddTransient<AgentManagementViewModel>();` in ConfigureServices method

## Phase 5: Chat Integration Updates

### Step 5.1: Update Chat Agent Loading

- **Modify**: `AgntsChatUI/ViewModels/ChatViewModel.cs`
  - Ensure LoadAgentsAsync method refreshes when new agents are added
  - Add event subscription to AgentManagementViewModel for agent changes

### Step 5.2: Update Main Window for Event Wiring

- **Modify**: `AgntsChatUI/ViewModels/MainWindowViewModel.cs`
  - Wire AgentManagementViewModel.AgentChanged event to ChatViewModel.LoadAgentsAsync

## Summary of File Operations

- **Add**: 8 new files
- **Modify**: 4 existing files
- **Remove**: 0 files

**New Files**: IFileTemplateService.cs, FileTemplateService.cs, AgentManagementViewModel.cs, AgentListComponent.axaml/.cs, AgentFormComponent.axaml/.cs, AgentManagementView.axaml/.cs

**Modified Files**: App.axaml.cs, MainWindowViewModel.cs, MainWindow.axaml, ChatViewModel.cs
