namespace AgntsChatUI.Views.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using AgntsChatUI.AI;
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Input;
    using Avalonia.Interactivity;
    using Avalonia.Controls.Primitives;

    public partial class CompactAgentSelector : UserControl
    {
        private ComboBox? _agentDropdown;
        private Button? _multiSelectToggle;
        private bool _isMultiSelectMode = false;

        public CompactAgentSelector()
        {
            this.InitializeComponent();
            this.Loaded += this.OnLoaded;
        }

        private void OnLoaded(object? sender, EventArgs e)
        {
            this._agentDropdown = this.FindControl<ComboBox>("AgentDropdown");
            this._multiSelectToggle = this.FindControl<Button>("MultiSelectToggle");

            // Set up keyboard navigation
            if (this._agentDropdown != null)
            {
                this._agentDropdown.KeyDown += this.OnAgentDropdownKeyDown;
            }

            // Set up focus management
            this.GotFocus += this.OnGotFocus;
        }

        private void OnAgentSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && 
                comboBox.SelectedItem is AgentDefinition agent && 
                this.DataContext is ChatViewModel viewModel)
            {
                if (this._isMultiSelectMode)
                {
                    // In multi-select mode, toggle the agent's selection
                    agent.IsSelected = !agent.IsSelected;
                    viewModel.OnAgentSelectionChanged(agent);
                    
                    // Clear the selection to allow for multiple selections
                    comboBox.SelectedItem = null;
                }
                else
                {
                    // In single-select mode, ensure only one agent remains selected
                    foreach (AgentDefinition other in viewModel.AvailableAgents)
                    {
                        if (!ReferenceEquals(other, agent) && other.IsSelected)
                        {
                            other.IsSelected = false;
                            viewModel.OnAgentSelectionChanged(other);
                        }
                    }

                    viewModel.SelectedAgents.Clear();
                    agent.IsSelected = true;
                    viewModel.SelectedAgents.Add(agent);
                    viewModel.OnAgentSelectionChanged(agent);
                    
                    // Update dropdown selection
                    this.UpdateDropdownSelection();
                }
            }
        }

        private void OnMultiSelectToggleClicked(object? sender, RoutedEventArgs e)
        {
            this._isMultiSelectMode = !this._isMultiSelectMode;
            
            if (this._multiSelectToggle != null)
            {
                // Update the button appearance to indicate mode
                if (this._multiSelectToggle?.Content is TextBlock textBlock)
                {
                    textBlock.Text = this._isMultiSelectMode ? "Single" : "Multi";
                }
                
                // Update tooltip
                if (this._multiSelectToggle != null)
                {
                    ToolTip.SetTip(this._multiSelectToggle, this._isMultiSelectMode 
                        ? "Switch to single agent selection" 
                        : "Toggle multi-agent selection");
                }
            }

            if (!this._isMultiSelectMode && this.DataContext is ChatViewModel vm)
            {
                // If switching back to single mode, keep only the first selected agent
                AgentDefinition? keep = vm.SelectedAgents.FirstOrDefault();
                foreach (AgentDefinition a in vm.SelectedAgents.ToList().Skip(1))
                {
                    vm.RemoveAgentSelection(a);
                }

                // Ensure all others are unselected in AvailableAgents
                foreach (AgentDefinition other in vm.AvailableAgents)
                {
                    if (!ReferenceEquals(other, keep) && other.IsSelected)
                    {
                        other.IsSelected = false;
                        vm.OnAgentSelectionChanged(other);
                    }
                }
            }

            // Update dropdown selection when switching modes
            this.UpdateDropdownSelection();
        }

        private void OnRemoveAgentClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is AgentDefinition agent && 
                this.DataContext is ChatViewModel viewModel)
            {
                viewModel.RemoveAgentSelection(agent);
            }
        }

        private void OnAgentDropdownKeyDown(object? sender, KeyEventArgs e)
        {
            if (this.DataContext is ChatViewModel viewModel)
            {
                switch (e.Key)
                {
                    case Key.Enter:
                        // Select the currently highlighted agent
                        if (this._agentDropdown?.SelectedItem is AgentDefinition agent)
                        {
                            this.OnAgentSelectionChanged(this._agentDropdown, new SelectionChangedEventArgs(ComboBox.SelectionChangedEvent, new List<object>(), new List<object>()));
                        }
                        e.Handled = true;
                        break;

                    case Key.Escape:
                        // Close the dropdown
                        if (this._agentDropdown != null)
                        {
                            this._agentDropdown.IsDropDownOpen = false;
                        }
                        e.Handled = true;
                        break;

                    case Key.Tab:
                        // Allow normal tab navigation
                        break;

                    case Key.Space:
                        // Toggle multi-select mode
                        this.OnMultiSelectToggleClicked(this._multiSelectToggle, new RoutedEventArgs());
                        e.Handled = true;
                        break;
                }
            }
        }

        private void OnGotFocus(object? sender, GotFocusEventArgs e)
        {
            // Set focus to the dropdown when the control gets focus
            if (this._agentDropdown != null && !this._agentDropdown.IsFocused)
            {
                this._agentDropdown.Focus();
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            // Handle DataContext changes to ensure proper binding
            if (this.DataContext is ChatViewModel viewModel)
            {
                // Update the UI to reflect the current state
                this.UpdateMultiSelectToggleState();
                this.UpdateDropdownSelection();
            }
        }

        private void UpdateMultiSelectToggleState()
        {
            if (this._multiSelectToggle != null)
            {
                // Reflect the current mode, not the number of selected agents
                if (this._multiSelectToggle.Content is TextBlock textBlock)
                {
                    textBlock.Text = this._isMultiSelectMode ? "Single" : "Multi";
                }

                ToolTip.SetTip(this._multiSelectToggle, this._isMultiSelectMode
                    ? "Switch to single agent selection"
                    : "Toggle multi-agent selection");
            }
        }

        private void UpdateDropdownSelection()
        {
            if (this._agentDropdown != null && this.DataContext is ChatViewModel viewModel)
            {
                if (!this._isMultiSelectMode && viewModel.SelectedAgents.Count > 0)
                {
                    // In single mode, show the first selected agent
                    this._agentDropdown.SelectedItem = viewModel.SelectedAgents[0];
                }
                else
                {
                    // In multi mode or no selection, clear the dropdown
                    this._agentDropdown.SelectedItem = null;
                }
            }
        }

        protected override void OnUnloaded(RoutedEventArgs e)
        {
            // Clean up event handlers
            if (this._agentDropdown != null)
            {
                this._agentDropdown.KeyDown -= this.OnAgentDropdownKeyDown;
            }

            this.GotFocus -= this.OnGotFocus;
            this.Loaded -= this.OnLoaded;

            base.OnUnloaded(e);
        }
    }
}
