# Task Overview

Replace the current single-agent selection system with multi-agent orchestration using Semantic Kernel's `SequentialOrchestration`. Users will be able to select multiple agents that will process their input sequentially, where each agent receives the input and the output from the previous agent is passed to the next agent in the chain.

---

## Phase 1: Package Dependencies and Infrastructure Setup

**Objective**: Add required packages and basic service infrastructure without breaking existing functionality.

**Additions:**

- `Microsoft.SemanticKernel.Agents.Orchestration` package reference to `AgntsChatUI.AI.csproj`
- `Microsoft.SemanticKernel.Agents.Runtime.InProcess` package reference to `AgntsChatUI.AI.csproj`

**Updates:**

- None

**Deletions:**

- None

**Summary**: Install orchestration packages to enable sequential agent processing. No code changes required, existing functionality remains intact.

---

## Phase 2: Data Model Updates

**Objective**: Modify view models to support multiple agent selection and orchestration state.

**Additions:**

- `SelectedAgents` property (ObservableCollection) to `ChatViewModel`
- `IsOrchestrationRunning` property (bool) to `ChatViewModel`

**Updates:**

- `SelectedAgent` property in `ChatViewModel` - change to computed property that returns first selected agent for backward compatibility
- `OnSelectedAgentChanged` method in `ChatViewModel` - update to handle collection changes

**Deletions:**

- None (maintain backward compatibility)

**Summary**: Extend data model to support multiple agent selection while preserving existing single-agent functionality through computed properties.

---

## Phase 3: Service Layer and Runtime Management

**Objective**: Refactor `ChatAgentFactory` to support dependency injection and orchestration runtime.

**Additions:**

- Service provider configuration in `ChatAgentFactory`
- `InProcessRuntime` service registration
- `InitializeRuntimeAsync` method in `ChatAgentFactory`
- `CreateOrchestration` method in `ChatAgentFactory`
- Runtime lifecycle management methods

**Updates:**

- `ChatAgentFactory` constructor - add service provider setup
- `CreateAgent` method - modify to use service-configured kernel
- Agent creation to use standardized `KernelArguments` with orchestration settings

**Deletions:**

- Direct kernel creation in agent factory methods

**Summary**: Transform factory into service-oriented architecture supporting orchestration runtime while maintaining existing agent creation API.

---

## Phase 4: UI Component Updates

**Objective**: Replace single agent ComboBox with multi-select interface.

**Additions:**

- CheckBox list control for agent selection in `ChatHeaderComponent.axaml`
- Agent selection count display
- Visual indicators for selected agents

**Updates:**

- `ChatHeaderComponent.axaml` - replace ComboBox with multi-select UI
- Agent selection binding from `SelectedAgent` to `SelectedAgents`
- Loading indicator logic to work with multiple agents

**Deletions:**

- Single agent ComboBox control
- Single agent selection display logic

**Summary**: Replace single-select agent UI with multi-select interface, showing selected agent count and providing clear selection feedback.

---

## Phase 5: Orchestration Execution Logic

**Objective**: Replace manual message processing with orchestration-based execution.

**Additions:**

- Orchestration creation logic in `SendMessage` method
- Runtime management calls (start/stop/idle)
- Orchestration result handling
- Orchestration error handling and fallback

**Updates:**

- `SendMessage` method in `ChatViewModel` - replace single agent invocation with orchestration
- Message streaming logic to work with orchestration results
- Error handling to manage orchestration failures
- `UpdateCurrentAgent` method renamed to `UpdateCurrentAgents` with orchestration setup

**Deletions:**

- Direct agent streaming invocation logic
- Manual response building and history management
- Single agent execution path

**Summary**: Replace direct agent invocation with Sequential Orchestration, maintaining streaming capabilities and error handling while processing multiple agents in sequence.
