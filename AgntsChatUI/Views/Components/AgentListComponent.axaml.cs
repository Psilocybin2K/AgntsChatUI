namespace AgntsChatUI.Views.Components
{
    using AgntsChatUI.AI;
    using AgntsChatUI.ViewModels;

    using Avalonia.Controls;
    using Avalonia.Interactivity;

    public partial class AgentListComponent : UserControl
    {
        public AgentListComponent()
        {
            this.InitializeComponent();
        }

        private void OnAgentSelected(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AgentDefinition agent && this.DataContext is AgentManagementViewModel viewModel)
            {
                viewModel.SelectedAgent = agent;
            }
        }
    }
}