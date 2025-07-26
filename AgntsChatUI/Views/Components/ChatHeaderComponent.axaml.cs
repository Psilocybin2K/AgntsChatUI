namespace AgntsChatUI.Views.Components
{
    using AgntsChatUI.AI;
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Interactivity;

    public partial class ChatHeaderComponent : UserControl
    {
        public ChatHeaderComponent()
        {
            this.InitializeComponent();
        }

        private void OnAgentCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is AgentDefinition agent && this.DataContext is ChatViewModel viewModel)
            {
                agent.IsSelected = true;
                viewModel.OnAgentSelectionChanged(agent);
            }
        }

        private void OnAgentCheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is AgentDefinition agent && this.DataContext is ChatViewModel viewModel)
            {
                agent.IsSelected = false;
                viewModel.OnAgentSelectionChanged(agent);
            }
        }

        private void OnRemoveAgentClicked(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AgentDefinition agent && this.DataContext is ChatViewModel viewModel)
            {
                viewModel.RemoveAgentSelection(agent);
            }
        }
    }
}